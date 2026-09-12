using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Testing.Toolkit.Http;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that answers from a factory instead of the network. The
/// factory sees the request, so it can vary the response, and it may throw to simulate a transport
/// failure. Every request is captured for later assertion.
/// </summary>
public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<CapturedHttpRequest, CancellationToken, Task<HttpResponseMessage>> _responseFactory;
    private readonly List<CapturedHttpRequest> _requests = [];
    private readonly Lock _gate = new();

    public MockHttpMessageHandler(
        Func<CapturedHttpRequest, CancellationToken, Task<HttpResponseMessage>> responseFactory
    )
    {
        ArgumentNullException.ThrowIfNull(responseFactory);

        _responseFactory = responseFactory;
    }

    public MockHttpMessageHandler(Func<CapturedHttpRequest, HttpResponseMessage> responseFactory)
    {
        ArgumentNullException.ThrowIfNull(responseFactory);

        _responseFactory = (request, _) => Task.FromResult(responseFactory(request));
    }

    /// <summary>Every request that reached the handler, oldest first.</summary>
    public IReadOnlyList<CapturedHttpRequest> Requests
    {
        get
        {
            lock (_gate)
            {
                return [.. _requests];
            }
        }
    }

    public CapturedHttpRequest? LastRequest
    {
        get
        {
            lock (_gate)
            {
                return _requests.Count == 0 ? null : _requests[^1];
            }
        }
    }

    public int CallCount
    {
        get
        {
            lock (_gate)
            {
                return _requests.Count;
            }
        }
    }

    /// <summary>Answers every request with the same status and body.</summary>
    public static MockHttpMessageHandler RespondWith(
        HttpStatusCode statusCode,
        string? content = null,
        string mediaType = MediaTypeNames.Application.Json
    ) => new(_ => Respond(statusCode, content, mediaType));

    /// <summary>Answers every request with the same JSON body, already serialized.</summary>
    public static MockHttpMessageHandler RespondWithJson(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK
    ) => new(_ => Respond(statusCode, json, MediaTypeNames.Application.Json));

    /// <summary>Answers every request with the same object, serialized per call.</summary>
    public static MockHttpMessageHandler RespondWithJson<TResponse>(
        TResponse body,
        HttpStatusCode statusCode = HttpStatusCode.OK
    ) => new(_ => RespondJson(body, statusCode));

    /// <summary>
    /// A response built fresh per call. Reusing one instance across calls fails, because reading its
    /// content the second time finds a disposed stream.
    /// </summary>
    public static HttpResponseMessage Respond(
        HttpStatusCode statusCode,
        string? content = null,
        string mediaType = MediaTypeNames.Application.Json
    ) => new(statusCode)
    {
        Content = content is null
            ? new StringContent(string.Empty)
            : new StringContent(content, Encoding.UTF8, mediaType)
    };

    /// <summary>A JSON response built fresh per call, serialized the way HttpClient reads it back.</summary>
    public static HttpResponseMessage RespondJson<TResponse>(
        TResponse body,
        HttpStatusCode statusCode = HttpStatusCode.OK
    ) => Respond(
        statusCode: statusCode,
        content: JsonSerializer.Serialize(body, JsonSerializerOptions.Web),
        mediaType: MediaTypeNames.Application.Json
    );

    /// <summary>Fails every request the way a transport error would.</summary>
    public static MockHttpMessageHandler ThrowsWith(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new MockHttpMessageHandler(_ => throw exception);
    }

    /// <summary>An <see cref="HttpClient"/> wired to this handler, which it does not own.</summary>
    public HttpClient CreateClient(Uri? baseAddress = null)
        => new(this, disposeHandler: false)
        {
            BaseAddress = baseAddress
        };

    public HttpClient CreateClient(string baseAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);

        return CreateClient(new Uri(baseAddress));
    }

    public void ClearRequests()
    {
        lock (_gate)
        {
            _requests.Clear();
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var captured = await CapturedHttpRequest.FromAsync(request, cancellationToken);

        lock (_gate)
        {
            _requests.Add(captured);
        }

        var response = await _responseFactory(captured, cancellationToken);

        response.RequestMessage ??= request;

        return response;
    }
}

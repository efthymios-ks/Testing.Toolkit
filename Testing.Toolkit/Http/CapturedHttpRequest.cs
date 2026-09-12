using System.Net.Http.Headers;
using System.Text;

namespace Testing.Toolkit.Http;

/// <summary>
/// A snapshot of a request taken as it was sent. <see cref="HttpClient"/> disposes the request and
/// its content once the call returns, so a test can only assert on a copy.
/// </summary>
public sealed class CapturedHttpRequest
{
    private readonly byte[] _content;

    internal CapturedHttpRequest(
        HttpMethod method,
        Uri? requestUri,
        Version version,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        IReadOnlyDictionary<string, IReadOnlyList<string>> contentHeaders,
        byte[] content
    )
    {
        Method = method;
        RequestUri = requestUri;
        Version = version;
        Headers = headers;
        ContentHeaders = contentHeaders;
        _content = content;
    }

    public HttpMethod Method { get; }

    public Uri? RequestUri { get; }

    public Version Version { get; }

    /// <summary>Request headers, such as Authorization or Accept.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }

    /// <summary>Content headers, such as Content-Type. These live on the body, not the request.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ContentHeaders { get; }

    public bool HasContent
        => _content.Length > 0;

    public string Content
        => Encoding.UTF8.GetString(_content);

    public byte[] ContentBytes
        => [.. _content];

    /// <summary>
    /// The first value of a header, looked up on the request and then on the content, or null when
    /// it was not sent. Header names are case-insensitive.
    /// </summary>
    public string? Header(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Headers.TryGetValue(name, out var values) || ContentHeaders.TryGetValue(name, out values))
        {
            return values.Count == 0 ? null : values[0];
        }

        return null;
    }

    public override string ToString()
        => $"{Method} {RequestUri}";

    internal static async Task<CapturedHttpRequest> FromAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var content = request.Content is null
            ? []
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);

        return new CapturedHttpRequest(
            method: request.Method,
            requestUri: request.RequestUri,
            version: request.Version,
            headers: Snapshot(request.Headers),
            contentHeaders: Snapshot(request.Content?.Headers),
            content: content
        );
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Snapshot(HttpHeaders? headers)
    {
        var snapshot = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
        {
            snapshot[header.Key] = [.. header.Value];
        }

        return snapshot;
    }
}

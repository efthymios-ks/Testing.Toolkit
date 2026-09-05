using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using Testing.Toolkit.Http;

namespace Testing.Toolkit.Tests.Http;

public sealed class MockHttpMessageHandlerTests
{
    [Fact]
    public async Task SendAsync_WhenFactoryReturnsAResponse_ShouldReturnIt()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.Respond(HttpStatusCode.Created, "{}"));
        using var client = handler.CreateClient("https://api.test");

        // Act
        using var response = await client.GetAsync("/orders");

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("{}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SendAsync_WhenFactoryReadsTheRequest_ShouldVaryTheResponse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(request => request.RequestUri!.AbsolutePath == "/orders"
            ? MockHttpMessageHandler.Respond(HttpStatusCode.OK)
            : MockHttpMessageHandler.Respond(HttpStatusCode.NotFound));
        using var client = handler.CreateClient("https://api.test");

        // Act
        using var found = await client.GetAsync("/orders");
        using var missing = await client.GetAsync("/customers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task SendAsync_WhenFactoryIsAsynchronous_ShouldAwaitIt()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(1, cancellationToken);

            return MockHttpMessageHandler.Respond(HttpStatusCode.Accepted);
        });
        using var client = handler.CreateClient("https://api.test");

        // Act
        using var response = await client.GetAsync("/orders");

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_WhenFactoryThrows_ShouldSurfaceTheException()
    {
        // Arrange
        var expected = new HttpRequestException("connection refused");
        var handler = MockHttpMessageHandler.ThrowsWith(expected);
        using var client = handler.CreateClient("https://api.test");

        // Act & Assert
        var thrown = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/orders"));

        Assert.Same(expected, thrown);
    }

    [Fact]
    public async Task SendAsync_WhenFactoryThrows_ShouldStillCaptureTheRequest()
    {
        // Arrange
        var handler = MockHttpMessageHandler.ThrowsWith(new HttpRequestException("connection refused"));
        using var client = handler.CreateClient("https://api.test");

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/orders"));

        Assert.Equal("/orders", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SendAsync_WhenRequestIsSent_ShouldCaptureMethodAndUri()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);
        using var client = handler.CreateClient("https://api.test");

        using var response = await client.PostAsJsonAsync("/orders", new { Reference = "A-1" });

        // Act
        var captured = handler.LastRequest!;

        // Assert
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal("https://api.test/orders", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task SendAsync_WhenRequestHasHeaders_ShouldCaptureThem()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);
        using var client = handler.CreateClient("https://api.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token");
        client.DefaultRequestHeaders.Add("X-Tenant", "acme");

        using var response = await client.GetAsync("/orders");

        // Act
        var captured = handler.LastRequest!;

        // Assert
        Assert.Equal("Bearer token", captured.Header("Authorization"));
        Assert.Equal("acme", captured.Header("x-tenant"));
    }

    [Fact]
    public async Task SendAsync_WhenRequestHasContent_ShouldCaptureTheBodyAndItsHeaders()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);
        using var client = handler.CreateClient("https://api.test");

        using var response = await client.PostAsJsonAsync("/orders", new { Reference = "A-1" });

        // Act
        var captured = handler.LastRequest!;

        // Assert
        Assert.True(captured.HasContent);
        Assert.Contains("A-1", captured.Content, StringComparison.Ordinal);
        Assert.Equal("application/json; charset=utf-8", captured.Header("Content-Type"));
    }

    [Fact]
    public async Task Content_WhenReadAfterTheClientDisposedTheRequest_ShouldStillBeAvailable()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);
        CapturedHttpRequest captured;

        // Act
        using (var client = handler.CreateClient("https://api.test"))
        {
            using var content = new StringContent("payload");
            using var response = await client.PostAsync("/orders", content);

            captured = handler.LastRequest!;
        }

        // Assert
        Assert.Equal("payload", captured.Content);
    }

    [Fact]
    public async Task SendAsync_WhenRequestHasNoContent_ShouldReportNone()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);
        using var client = handler.CreateClient("https://api.test");

        using var response = await client.GetAsync("/orders");

        // Act
        var captured = handler.LastRequest!;

        // Assert
        Assert.False(captured.HasContent);
        Assert.Equal(string.Empty, captured.Content);
    }

    [Fact]
    public async Task SendAsync_WhenCalledSeveralTimes_ShouldCaptureEveryRequestOldestFirst()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);
        using var client = handler.CreateClient("https://api.test");

        // Act
        using var first = await client.GetAsync("/orders");
        using var second = await client.GetAsync("/customers");

        // Assert
        Assert.Equal(2, handler.CallCount);
        Assert.Equal(
            ["/orders", "/customers"],
            handler.Requests.Select(request => request.RequestUri!.AbsolutePath)
        );
    }

    [Fact]
    public async Task RespondWith_WhenCalledMoreThanOnce_ShouldGiveEachCallItsOwnResponse()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK, "body");
        using var client = handler.CreateClient("https://api.test");

        // Act
        using var first = await client.GetAsync("/orders");
        using var second = await client.GetAsync("/orders");

        // Assert
        Assert.Equal("body", await first.Content.ReadAsStringAsync());
        Assert.Equal("body", await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SendAsync_WhenResponding_ShouldAttachTheRequestToTheResponse()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);
        using var client = handler.CreateClient("https://api.test");

        // Act
        using var response = await client.GetAsync("/orders");

        // Assert
        Assert.Equal("https://api.test/orders", response.RequestMessage!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ClearRequests_WhenCalled_ShouldForgetWhatWasCaptured()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);
        using var client = handler.CreateClient("https://api.test");

        using var response = await client.GetAsync("/orders");

        // Act
        handler.ClearRequests();

        // Assert
        Assert.Equal(0, handler.CallCount);
        Assert.Null(handler.LastRequest);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void LastRequest_WhenNothingWasSent_ShouldBeNull()
    {
        // Arrange & Act
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);

        // Assert
        Assert.Null(handler.LastRequest);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ContentBytes_WhenChanged_ShouldNotAffectTheCapturedRequest()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);
        using var client = handler.CreateClient("https://api.test");
        using var content = new StringContent("payload");

        using var response = await client.PostAsync("/orders", content);

        // Act
        var captured = handler.LastRequest!;
        captured.ContentBytes[0] = 0;

        // Assert
        Assert.Equal("payload", captured.Content);
    }

    [Fact]
    public void CreateClient_WhenNoBaseAddressIsGiven_ShouldLeaveItUnset()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);

        // Act
        using var client = handler.CreateClient();

        // Assert
        Assert.Null(client.BaseAddress);
    }

    [Fact]
    public async Task CreateClient_WhenTheClientIsDisposed_ShouldLeaveTheHandlerUsable()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);

        using (var first = handler.CreateClient("https://api.test"))
        {
            using var response = await first.GetAsync("/orders");
        }

        // Act
        using var second = handler.CreateClient("https://api.test");
        using var reused = await second.GetAsync("/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, reused.StatusCode);
    }

    [Fact]
    public void Header_WhenTheHeaderWasNotSent_ShouldReturnNull()
    {
        // Arrange & Act
        var captured = new CapturedHttpRequest(
            method: HttpMethod.Get,
            requestUri: new Uri("https://api.test/orders"),
            version: HttpVersion.Version11,
            headers: new Dictionary<string, IReadOnlyList<string>>(),
            contentHeaders: new Dictionary<string, IReadOnlyList<string>>(),
            content: []
        );

        // Assert
        Assert.Null(captured.Header("Authorization"));
    }

    [Fact]
    public void Header_WhenNameIsEmpty_ShouldThrow()
    {
        // Arrange
        var captured = new CapturedHttpRequest(
            method: HttpMethod.Get,
            requestUri: new Uri("https://api.test/orders"),
            version: HttpVersion.Version11,
            headers: new Dictionary<string, IReadOnlyList<string>>(),
            contentHeaders: new Dictionary<string, IReadOnlyList<string>>(),
            content: []
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => captured.Header(" "));
    }

    [Fact]
    public void ToString_WhenCalled_ShouldReadAsMethodAndUri()
    {
        // Arrange & Act
        var captured = new CapturedHttpRequest(
            method: HttpMethod.Post,
            requestUri: new Uri("https://api.test/orders"),
            version: HttpVersion.Version11,
            headers: new Dictionary<string, IReadOnlyList<string>>(),
            contentHeaders: new Dictionary<string, IReadOnlyList<string>>(),
            content: []
        );

        // Assert
        Assert.Equal("POST https://api.test/orders", captured.ToString());
    }

    [Fact]
    public async Task RespondWithJson_WhenGivenAnObject_ShouldSerializeItPerCall()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWithJson(new Order("A-1"), HttpStatusCode.Created);
        using var client = handler.CreateClient("https://api.test");

        // Act
        using var response = await client.GetAsync("/orders");
        var body = await response.Content.ReadFromJsonAsync<Order>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("A-1", body!.Reference);
        Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task RespondWithJson_WhenGivenAJsonString_ShouldReturnItVerbatim()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWithJson("""{"reference":"A-1"}""");
        using var client = handler.CreateClient("https://api.test");

        // Act
        using var response = await client.GetAsync("/orders");

        // Assert
        Assert.Equal("""{"reference":"A-1"}""", await response.Content.ReadAsStringAsync());
        Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task RespondJson_WhenCalledMoreThanOnce_ShouldGiveEachCallItsOwnResponse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => MockHttpMessageHandler.RespondJson(new Order("A-1")));
        using var client = handler.CreateClient("https://api.test");

        // Act
        using var first = await client.GetAsync("/orders");
        using var second = await client.GetAsync("/orders");

        // Assert
        Assert.Equal("""{"reference":"A-1"}""", await first.Content.ReadAsStringAsync());
        Assert.Equal("""{"reference":"A-1"}""", await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public void Constructor_WhenFactoryIsNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new MockHttpMessageHandler((Func<CapturedHttpRequest, HttpResponseMessage>)null!)
        );
    }

    [Fact]
    public void Constructor_WhenAsyncFactoryIsNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new MockHttpMessageHandler(
                (Func<CapturedHttpRequest, CancellationToken, Task<HttpResponseMessage>>)null!
            )
        );
    }

    [Fact]
    public async Task SendAsync_WhenHeaderHasSeveralValues_ShouldCaptureEachOfThem()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);
        using var client = handler.CreateClient("https://api.test");
        client.DefaultRequestHeaders.Add("X-Tag", new[] { "first", "second" });

        using var response = await client.GetAsync("/orders");

        // Act
        var captured = handler.LastRequest!;

        // Assert
        Assert.Equal(["first", "second"], captured.Headers["X-Tag"]);
        Assert.Equal("first", captured.Header("X-Tag"));
    }

    [Fact]
    public async Task SendAsync_WhenRequestIsSent_ShouldCaptureItsVersionAndContentHeaders()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);
        using var client = handler.CreateClient("https://api.test");
        using var content = new StringContent("payload");

        using var response = await client.PostAsync("/orders", content);

        // Act
        var captured = handler.LastRequest!;

        // Assert
        Assert.Equal(client.DefaultRequestVersion, captured.Version);
        Assert.Contains("Content-Type", captured.ContentHeaders.Keys);
        Assert.DoesNotContain("Content-Type", captured.Headers.Keys);
    }

    [Fact]
    public void ThrowsWith_WhenExceptionIsNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => MockHttpMessageHandler.ThrowsWith(null!));
    }

    [Fact]
    public void CreateClient_WhenBaseAddressIsEmpty_ShouldThrow()
    {
        // Arrange
        var handler = MockHttpMessageHandler.RespondWith(HttpStatusCode.OK);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => handler.CreateClient(" "));
    }

    private sealed record Order(string Reference);
}

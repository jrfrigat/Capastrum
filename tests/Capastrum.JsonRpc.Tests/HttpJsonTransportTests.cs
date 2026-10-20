using System.Net;
using System.Text;
using System.Text.Json;
using Capastrum.JsonRpc.Transports;
using Xunit;

namespace Capastrum.JsonRpc.Tests;

public sealed class HttpJsonTransportTests
{
    [Fact]
    public async Task PostsRequestAndReadsResponse()
    {
        string? method = null;
        string? header = null;

        using var client = new HttpClient(new StubHandler(async request =>
        {
            header = request.Headers.TryGetValues("X-Test", out var values) ? values.Single() : null;
            var body = await request.Content!.ReadAsStringAsync();
            var message = JsonSerializer.Deserialize<JsonRpcMessage>(body);
            method = Assert.IsType<JsonRpcRequest>(message).Method;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"jsonrpc":"2.0","id":1,"result":{"ok":true}}""",
                    Encoding.UTF8,
                    "application/json"),
            };
        }));

        await using var transport = new HttpJsonTransport(client, new Uri("https://example.test/rpc"), ownsClient: false)
        {
            ConfigureRequest = (request, _) => request.Headers.Add("X-Test", "value"),
        };

        await transport.SendAsync(new JsonRpcRequest { Id = new JsonRpcId(1), Method = "tools/list" });

        var enumerator = transport.ReadAsync().GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));

        var response = Assert.IsType<JsonRpcResponse>(enumerator.Current);
        Assert.True(response.Result?.GetProperty("ok").GetBoolean());
        Assert.Equal("tools/list", method);
        Assert.Equal("value", header);
    }

    [Fact]
    public async Task NotificationDoesNotExpectResponse()
    {
        var called = false;

        using var client = new HttpClient(new StubHandler(_ =>
        {
            called = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }));

        await using var transport = new HttpJsonTransport(client, new Uri("https://example.test/rpc"));

        await transport.SendAsync(new JsonRpcNotification { Method = "notifications/cancelled" });

        Assert.True(called);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => handler(request);
    }
}

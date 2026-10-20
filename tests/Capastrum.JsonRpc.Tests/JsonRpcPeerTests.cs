using System.Text.Json;
using Xunit;

namespace Capastrum.JsonRpc.Tests;

public sealed class JsonRpcPeerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task InvokeReturnsResult()
    {
        var (clientTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        await using var client = new JsonRpcPeer(clientTransport);
        await using var server = new JsonRpcPeer(serverTransport);

        server.RequestHandler = (request, _) => ValueTask.FromResult(new JsonRpcResponse
        {
            Id = request.Id,
            Result = JsonSerializer.SerializeToElement(new { value = 42 }),
        });

        var result = await client.InvokeAsync("get").AsTask().WaitAsync(Timeout);

        Assert.Equal(42, result.GetProperty("value").GetInt32());
    }

    [Fact]
    public async Task InvokeThrowsJsonRpcException()
    {
        var (clientTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        await using var client = new JsonRpcPeer(clientTransport);
        await using var server = new JsonRpcPeer(serverTransport);

        server.RequestHandler = (_, _) => throw new JsonRpcException(
            -32000,
            "denied",
            JsonSerializer.SerializeToElement(new { reason = "test" }));

        var exception = await Assert.ThrowsAsync<JsonRpcException>(
            () => client.InvokeAsync("fail").AsTask().WaitAsync(Timeout));

        Assert.Equal(-32000, exception.Code);
        Assert.Equal("denied", exception.Message);
        Assert.Equal("test", exception.Data?.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task MissingHandlerReturnsMethodNotFound()
    {
        var (clientTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        await using var client = new JsonRpcPeer(clientTransport);
        await using var server = new JsonRpcPeer(serverTransport);

        var exception = await Assert.ThrowsAsync<JsonRpcException>(
            () => client.InvokeAsync("missing").AsTask().WaitAsync(Timeout));

        Assert.Equal(JsonRpcErrorCode.MethodNotFound, exception.Code);
    }

    [Fact]
    public async Task NotificationIsRaised()
    {
        var (clientTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        await using var client = new JsonRpcPeer(clientTransport);
        await using var server = new JsonRpcPeer(serverTransport);

        var received = new TaskCompletionSource<JsonRpcNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        client.NotificationReceived += notification => received.TrySetResult(notification);

        await server.NotifyAsync("progress", JsonSerializer.SerializeToElement(new { value = 1 }));

        var notification = await received.Task.WaitAsync(Timeout);
        Assert.Equal("progress", notification.Method);
        Assert.Equal(1, notification.Params?.GetProperty("value").GetInt32());
    }

    [Fact]
    public async Task CancellationStopsWaiting()
    {
        var (clientTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        await using var client = new JsonRpcPeer(clientTransport);
        await using var server = new JsonRpcPeer(serverTransport);

        server.RequestHandler = async (request, cancellationToken) =>
        {
            await Task.Delay(Timeout, cancellationToken);
            return new JsonRpcResponse { Id = request.Id };
        };

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.InvokeAsync("slow", cancellationToken: cancellation.Token).AsTask());
    }

    [Fact]
    public async Task ConcurrentRequestsAreCorrelated()
    {
        var (clientTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        await using var client = new JsonRpcPeer(clientTransport);
        await using var server = new JsonRpcPeer(serverTransport);

        server.RequestHandler = (request, _) =>
        {
            var value = request.Params?.GetProperty("value").GetInt32() ?? 0;
            return ValueTask.FromResult(new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(new { value = value * 2 }),
            });
        };

        var tasks = Enumerable.Range(1, 50)
            .Select(value => client.InvokeAsync(
                "double",
                JsonSerializer.SerializeToElement(new { value })).AsTask())
            .ToArray();

        var results = await Task.WhenAll(tasks).WaitAsync(Timeout);

        Assert.Equal(2, results[0].GetProperty("value").GetInt32());
        Assert.Equal(100, results[49].GetProperty("value").GetInt32());
    }

    [Fact]
    public async Task DisposeFailsPendingRequests()
    {
        var (clientTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        var client = new JsonRpcPeer(clientTransport);
        await using var server = new JsonRpcPeer(serverTransport);

        server.RequestHandler = async (request, cancellationToken) =>
        {
            await Task.Delay(Timeout, cancellationToken);
            return new JsonRpcResponse { Id = request.Id };
        };

        var pending = client.InvokeAsync("slow").AsTask();

        await client.DisposeAsync();

        await Assert.ThrowsAnyAsync<ObjectDisposedException>(() => pending.WaitAsync(Timeout));
    }
}

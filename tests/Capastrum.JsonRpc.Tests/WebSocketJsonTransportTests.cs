using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using Capastrum.JsonRpc.Transports;
using Xunit;

namespace Capastrum.JsonRpc.Tests;

public sealed class WebSocketJsonTransportTests
{
    [Fact]
    public async Task RoundTripsNotification()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;

        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(endpoint.Address, endpoint.Port);
        using var accepted = await listener.AcceptTcpClientAsync();
        await connectTask;

        using var serverSocket = WebSocket.CreateFromStream(
            accepted.GetStream(),
            isServer: true,
            subProtocol: null,
            keepAliveInterval: TimeSpan.Zero);
        using var clientSocket = WebSocket.CreateFromStream(
            client.GetStream(),
            isServer: false,
            subProtocol: null,
            keepAliveInterval: TimeSpan.Zero);

        await using var clientTransport = new WebSocketJsonTransport(clientSocket);
        await using var serverTransport = new WebSocketJsonTransport(serverSocket);

        await clientTransport.SendAsync(new JsonRpcNotification { Method = "hello" });
        await clientTransport.SendAsync(new JsonRpcNotification { Method = "world" });

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var enumerator = serverTransport.ReadAsync(cancellation.Token).GetAsyncEnumerator(cancellation.Token);

        Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
        var first = Assert.IsType<JsonRpcNotification>(enumerator.Current);

        Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
        var second = Assert.IsType<JsonRpcNotification>(enumerator.Current);

        Assert.Equal("hello", first.Method);
        Assert.Equal("world", second.Method);
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Capastrum.JsonRpc.Tests;

public sealed record AddRequest(int Left, int Right);

public sealed record AddResult(int Sum);

[JsonSerializable(typeof(AddRequest))]
[JsonSerializable(typeof(AddResult))]
internal sealed partial class AddJsonContext : JsonSerializerContext;

public sealed class JsonRpcPeerTypedTests
{
    [Fact]
    public async Task TypedParametersAndResultRoundTrip()
    {
        var (clientTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        await using var client = new JsonRpcPeer(clientTransport);
        await using var server = new JsonRpcPeer(serverTransport);

        server.RequestHandler = (request, _) =>
        {
            var input = request.Params!.Value.Deserialize(AddJsonContext.Default.AddRequest)!;
            return ValueTask.FromResult(new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(
                    new AddResult(input.Left + input.Right),
                    AddJsonContext.Default.AddResult),
            });
        };

        var raw = await client
            .InvokeAsync("add", new AddRequest(2, 3), AddJsonContext.Default.AddRequest)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5));

        var result = raw.Deserialize(AddJsonContext.Default.AddResult);

        Assert.Equal(5, result?.Sum);
    }

    [Fact]
    public async Task TypedResultOverloadDeserializes()
    {
        var (clientTransport, serverTransport) = InMemoryJsonRpcTransport.CreatePair();
        await using var client = new JsonRpcPeer(clientTransport);
        await using var server = new JsonRpcPeer(serverTransport);

        server.RequestHandler = (request, _) => ValueTask.FromResult(new JsonRpcResponse
        {
            Id = request.Id,
            Result = JsonSerializer.SerializeToElement(new AddResult(7), AddJsonContext.Default.AddResult),
        });

        var result = await client
            .InvokeAsync("seven", (JsonElement?)null, AddJsonContext.Default.AddResult)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(7, result?.Sum);
    }
}

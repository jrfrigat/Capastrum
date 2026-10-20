using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Capastrum.JsonRpc;

namespace Capastrum.Benchmarks;

/// <summary>
/// Measures a full JSON-RPC request/response round trip over an in-memory transport.
/// </summary>
[MemoryDiagnoser]
public class JsonRpcPeerBenchmarks : IDisposable
{
    private JsonRpcPeer _client = null!;
    private JsonRpcPeer _server = null!;

    /// <summary>
    /// Creates a connected pair of peers.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var (clientTransport, serverTransport) = InMemoryBenchmarkTransport.CreatePair();

        _client = new JsonRpcPeer(clientTransport);
        _server = new JsonRpcPeer(serverTransport);
        _server.RequestHandler = (request, _) => ValueTask.FromResult(new JsonRpcResponse
        {
            Id = request.Id,
            Result = JsonSerializer.SerializeToElement(new { ok = true }),
        });
    }

    /// <summary>
    /// Releases the peers.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup() => Dispose();

    /// <summary>
    /// Sends a request and waits for its response.
    /// </summary>
    [Benchmark]
    public async Task<JsonElement> InvokeRoundTrip() => await _client.InvokeAsync("ping");

    /// <inheritdoc />
    public void Dispose()
    {
        _client.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _server.DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Capastrum.JsonRpc;

namespace Capastrum.Benchmarks;

/// <summary>
/// Measures serialization and deserialization of JSON-RPC envelopes.
/// </summary>
[MemoryDiagnoser]
public class JsonRpcEnvelopeBenchmarks
{
    private readonly JsonRpcRequest _request;
    private readonly JsonRpcResponse _response;
    private readonly byte[] _requestBytes;
    private readonly byte[] _responseBytes;

    /// <summary>
    /// Creates the envelopes used by every benchmark.
    /// </summary>
    public JsonRpcEnvelopeBenchmarks()
    {
        _request = new JsonRpcRequest
        {
            Id = new JsonRpcId(1),
            Method = "tools/call",
            Params = JsonSerializer.SerializeToElement(new { name = "echo", arguments = new { value = 42 } }),
        };

        _response = new JsonRpcResponse
        {
            Id = new JsonRpcId(1),
            Result = JsonSerializer.SerializeToElement(new { content = "ok" }),
        };

        _requestBytes = JsonSerializer.SerializeToUtf8Bytes<JsonRpcMessage>(_request);
        _responseBytes = JsonSerializer.SerializeToUtf8Bytes<JsonRpcMessage>(_response);
    }

    /// <summary>Serializes a request envelope.</summary>
    [Benchmark]
    public byte[] SerializeRequest() => JsonSerializer.SerializeToUtf8Bytes<JsonRpcMessage>(_request);

    /// <summary>Serializes a response envelope.</summary>
    [Benchmark]
    public byte[] SerializeResponse() => JsonSerializer.SerializeToUtf8Bytes<JsonRpcMessage>(_response);

    /// <summary>Deserializes a request envelope.</summary>
    [Benchmark]
    public JsonRpcMessage? DeserializeRequest() => JsonSerializer.Deserialize<JsonRpcMessage>(_requestBytes);

    /// <summary>Deserializes a response envelope.</summary>
    [Benchmark]
    public JsonRpcMessage? DeserializeResponse() => JsonSerializer.Deserialize<JsonRpcMessage>(_responseBytes);
}

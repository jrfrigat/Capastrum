using System.Text.Json;
using BenchmarkDotNet.Attributes;

namespace Capastrum.Benchmarks;

/// <summary>
/// Measures the cost of parsing a JSON-RPC envelope and reading its id.
/// </summary>
[MemoryDiagnoser]
public class JsonPayloadBenchmarks
{
    private static readonly byte[] Payload = JsonSerializer.SerializeToUtf8Bytes(
        new BenchmarkEnvelope("tools/call", 1, "sample"));

    private readonly byte[] _payload = Payload;

    /// <summary>
    /// Parses the envelope and returns the request id.
    /// </summary>
    [Benchmark]
    public int DeserializeEnvelope()
    {
        using var document = JsonDocument.Parse(_payload);
        return document.RootElement.GetProperty("id").GetInt32();
    }

    /// <summary>
    /// A minimal JSON-RPC request used by the benchmark.
    /// </summary>
    public sealed record BenchmarkEnvelope(string Method, int Id, string Name);
}

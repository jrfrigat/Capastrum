using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Capastrum.JsonRpc;
using Capastrum.Mcp;
using Capastrum.Mcp.Client;
using Capastrum.Mcp.Protocol;

namespace Capastrum.Benchmarks;

/// <summary>
/// Measures a full MCP tool call through the server and client.
/// </summary>
[MemoryDiagnoser]
public class McpServerBenchmarks : IAsyncDisposable, IDisposable
{
    private JsonRpcPeer? _serverPeer;
    private McpServerSession? _session;
    private McpClient? _client;

    /// <summary>
    /// Creates the MCP server and client pair.
    /// </summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var server = new McpServer(new McpServerOptions { Name = "bench", Version = "1.0.0" });
        server.RegisterTool(
            "echo",
            "Echoes a value",
            (context, _) => ValueTask.FromResult<McpResult>(
                McpToolResult.Text(context.Arguments.GetProperty("value").GetString()!)));

        var (clientTransport, serverTransport) = InMemoryBenchmarkTransport.CreatePair();
        _serverPeer = new JsonRpcPeer(serverTransport);
        _session = server.CreateSession(
            (notification, cancellationToken) => _serverPeer.NotifyAsync(
                notification.Method,
                notification.Params,
                cancellationToken));

        _serverPeer.RequestHandler = _session.HandleRequestAsync;
        _serverPeer.NotificationReceived += _session.HandleNotification;
        _client = await McpClient.ConnectAsync(clientTransport, new McpClientOptions());
    }

    /// <summary>
    /// Releases the server and client.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup() => Dispose();

    /// <summary>
    /// Calls the echo tool.
    /// </summary>
    [Benchmark]
    public async Task<McpCallToolResult> CallTool() =>
        await _client!.CallToolAsync(
            "echo",
            JsonSerializer.SerializeToElement(new { value = "hello" }));

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        if (_session is not null)
        {
            await _session.DisposeAsync();
        }

        if (_serverPeer is not null)
        {
            await _serverPeer.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}

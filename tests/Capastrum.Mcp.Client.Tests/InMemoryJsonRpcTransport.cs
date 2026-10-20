using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Capastrum.JsonRpc;

namespace Capastrum.Mcp.Client.Tests;

internal sealed class InMemoryJsonRpcTransport : IJsonRpcTransport
{
    private readonly Channel<JsonRpcMessage> _incoming;
    private readonly ChannelWriter<JsonRpcMessage> _outgoing;

    private InMemoryJsonRpcTransport(Channel<JsonRpcMessage> incoming, ChannelWriter<JsonRpcMessage> outgoing)
    {
        _incoming = incoming;
        _outgoing = outgoing;
    }

    public static (InMemoryJsonRpcTransport Client, InMemoryJsonRpcTransport Server) CreatePair()
    {
        var clientIncoming = Channel.CreateUnbounded<JsonRpcMessage>();
        var serverIncoming = Channel.CreateUnbounded<JsonRpcMessage>();

        return (
            new InMemoryJsonRpcTransport(clientIncoming, serverIncoming.Writer),
            new InMemoryJsonRpcTransport(serverIncoming, clientIncoming.Writer));
    }

    public ValueTask SendAsync(JsonRpcMessage message, CancellationToken cancellationToken = default) =>
        _outgoing.WriteAsync(message, cancellationToken);

    public async IAsyncEnumerable<JsonRpcMessage> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in _incoming.Reader.ReadAllAsync(cancellationToken))
        {
            yield return message;
        }
    }

    public ValueTask DisposeAsync()
    {
        _incoming.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}

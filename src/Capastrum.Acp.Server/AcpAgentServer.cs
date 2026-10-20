using Capastrum.JsonRpc;
using Capastrum.JsonRpc.Transports;

namespace Capastrum.Acp.Server;

/// <summary>
/// An ACP agent server that exposes a C# application as an agent.
/// </summary>
public sealed class AcpAgentServer
{
    private readonly AcpAgentServerOptions _options;

    /// <summary>Creates a server with the given options.</summary>
    public AcpAgentServer(AcpAgentServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <summary>The server options.</summary>
    public AcpAgentServerOptions Options => _options;

    /// <summary>Creates a session bound to a peer.</summary>
    public AcpAgentSession CreateSession(JsonRpcPeer peer)
    {
        ArgumentNullException.ThrowIfNull(peer);

        return new AcpAgentSession(_options, peer);
    }

    /// <summary>Runs a stdio session until the input stream closes or the token is cancelled.</summary>
    public async Task RunAsync(Stream input, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        await using var transport = new NewlineJsonTransport(input, output);
        await using var peer = new JsonRpcPeer(transport);
        await using var session = CreateSession(peer);

        peer.RequestHandler = session.HandleRequestAsync;
        peer.NotificationReceived += session.HandleNotification;

        try
        {
            await peer.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}

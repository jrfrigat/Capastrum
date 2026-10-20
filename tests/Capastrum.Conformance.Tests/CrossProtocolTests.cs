using System.Text.Json;
using Capastrum.Acp;
using Capastrum.Acp.Protocol;
using Capastrum.Acp.Server;
using Capastrum.JsonRpc;
using Capastrum.Mcp;
using Capastrum.Mcp.Client;
using Capastrum.Mcp.Protocol;
using Xunit;

namespace Capastrum.Conformance.Tests;

public sealed class CrossProtocolTests
{
    [Fact]
    public async Task AcpHostCallsMcpToolThroughAgent()
    {
        var mcpServer = new McpServer(new McpServerOptions { Name = "mcp", Version = "1.0.0" });
        mcpServer.RegisterTool(
            "echo",
            "Echoes a value",
            (context, _) => ValueTask.FromResult<McpResult>(
                McpToolResult.Text(context.Arguments.GetProperty("value").GetString()!)));

        var (mcpClientTransport, mcpServerTransport) = InMemoryJsonRpcTransport.CreatePair();
        var mcpServerPeer = new JsonRpcPeer(mcpServerTransport);
        var mcpSession = mcpServer.CreateSession(
            (notification, cancellationToken) => mcpServerPeer.NotifyAsync(
                notification.Method,
                notification.Params,
                cancellationToken));

        mcpServerPeer.RequestHandler = mcpSession.HandleRequestAsync;
        mcpServerPeer.NotificationReceived += mcpSession.HandleNotification;

        await using var mcpSessionLifetime = mcpSession;
        await using var mcpServerPeerLifetime = mcpServerPeer;
        await using var mcpClient = await McpClient.ConnectAsync(
            mcpClientTransport,
            new McpClientOptions());

        var agentServer = new AcpAgentServer(new AcpAgentServerOptions
        {
            AgentInfo = new AcpImplementation { Name = "agent", Version = "1.0.0" },
            NewSessionHandler = (_, _) => ValueTask.FromResult(new AcpNewSessionResponse
            {
                SessionId = "session-1",
            }),
            PromptHandler = async (context, cancellationToken) =>
            {
                var toolResult = await mcpClient.CallToolAsync(
                    "echo",
                    JsonSerializer.SerializeToElement(new { value = "hello" }),
                    cancellationToken: cancellationToken);

                var text = Assert.IsType<McpTextContent>(toolResult.Content[0]).Text;

                await context.SendUpdateAsync(
                    new AcpAgentMessageChunk
                    {
                        Content = new AcpTextContent { Text = text },
                    },
                    cancellationToken);

                return new AcpPromptResponse { StopReason = AcpStopReason.EndTurn };
            },
        });

        var (hostTransport, agentTransport) = InMemoryJsonRpcTransport.CreatePair();
        var agentPeer = new JsonRpcPeer(agentTransport);
        var agentSession = agentServer.CreateSession(agentPeer);
        agentPeer.RequestHandler = agentSession.HandleRequestAsync;
        agentPeer.NotificationReceived += agentSession.HandleNotification;

        await using var agentSessionLifetime = agentSession;
        await using var agentPeerLifetime = agentPeer;
        await using var host = await AcpAgentClient.ConnectAsync(
            hostTransport,
            new AcpClientOptions { WorkspaceRoot = Path.GetTempPath() });

        AcpSessionNotification? update = null;
        host.OnSessionUpdate(notification => update = notification);

        var response = await host.SendPromptAsync(
            "session-1",
            new[] { new AcpTextContent { Text = "go" } });

        Assert.Equal(AcpStopReason.EndTurn, response.StopReason);
        Assert.Equal("hello", update?.Text);
    }
}

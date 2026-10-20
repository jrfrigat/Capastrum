using Capastrum.Acp.Protocol;
using Capastrum.Acp.Server;

var server = new AcpAgentServer(new AcpAgentServerOptions
{
    AgentInfo = new AcpImplementation
    {
        Name = "Capastrum.AcpAgentSample",
        Version = "1.0.0",
    },
    NewSessionHandler = (_, _) => ValueTask.FromResult(new AcpNewSessionResponse
    {
        SessionId = Guid.NewGuid().ToString("N"),
    }),
    PromptHandler = async (context, cancellationToken) =>
    {
        await context.SendUpdateAsync(
            new AcpAgentMessageChunk
            {
                Content = new AcpTextContent { Text = "Hello from Capastrum." },
            },
            cancellationToken);

        return new AcpPromptResponse { StopReason = AcpStopReason.EndTurn };
    },
});

await server.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());

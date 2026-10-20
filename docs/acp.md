# ACP

`Capastrum.Acp.Protocol` contains the ACP v1 contracts; `Capastrum.Acp` is the host and `Capastrum.Acp.Server` is the agent side. Both use `AcpJsonContext`.

## Host

```csharp
await using var agent = await AcpAgentClient.StartAsync(new AcpClientOptions
{
    Executable = "npx",
    Arguments = ["-y", "@zed-industries/claude-code-acp"],
    WorkspaceRoot = "/path/to/project",
});

agent.OnSessionUpdate(update => Console.WriteLine(update.Text));
agent.OnActionConfirm((request, _) => ValueTask.FromResult(
    request.Command?.Contains("rm -rf", StringComparison.Ordinal) == true
        ? ConfirmationResult.Denied
        : ConfirmationResult.Allowed));

var session = await agent.NewSessionAsync("/path/to/project");
var response = await agent.SendPromptAsync(
    session.SessionId,
    [new AcpTextContent { Text = "Find outdated dependencies." }]);
```

`StartAsync` launches the process, connects over stdio and performs `initialize`. `ConnectAsync` accepts an existing `IJsonRpcTransport` for tests or custom transports.

### Human-in-the-loop

`OnActionConfirm` receives an `AcpPermissionRequest` with the session, tool call and permission options. It returns `ConfirmationResult.Allowed`, `Denied` or `Selected(optionId)`. Without a handler, the host denies the action; if no matching allow/reject option exists, the outcome is `cancelled`.

### Files and terminals

`OnReadTextFile` and `OnWriteTextFile` register explicit file handlers. Without them, the agent receives a JSON-RPC error. `AcpTerminalHost` starts processes, buffers output with a limit and implements `terminal/output`, `wait_for_exit`, `kill` and `release`; the host creates one by default and owns its lifetime.

### Updates and cancellation

`session/update` notifications raise `SessionUpdated`; `update.Text` returns text chunks. `CancelAsync(sessionId)` sends `session/cancel`; cancelling the token passed to `SendPromptAsync` does the same. The agent returns `stopReason=cancelled`.

## Agent

```csharp
var server = new AcpAgentServer(new AcpAgentServerOptions
{
    AgentInfo = new AcpImplementation { Name = "MyAgent", Version = "1.0.0" },
    NewSessionHandler = (_, _) => ValueTask.FromResult(new AcpNewSessionResponse
    {
        SessionId = Guid.NewGuid().ToString("N"),
    }),
    PromptHandler = async (context, cancellationToken) =>
    {
        await context.SendUpdateAsync(
            new AcpAgentMessageChunk
            {
                Content = new AcpTextContent { Text = "Hello from C#." },
            },
            cancellationToken);

        return new AcpPromptResponse { StopReason = AcpStopReason.EndTurn };
    },
});

await server.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());
```

`AcpPromptContext` exposes `SendUpdateAsync`, `RequestPermissionAsync`, `ReadTextFileAsync`, `WriteTextFileAsync`, `CreateTerminalAsync`, `GetTerminalOutputAsync`, `WaitForTerminalExitAsync`, `KillTerminalAsync`, `ReleaseTerminalAsync` and `ElicitAsync`. Missing optional handlers produce method-not-found responses; `PromptHandler` is required.

## Coverage

`initialize`, `authenticate`, `logout`, `session/new`, `session/load`, `session/resume`, `session/list`, `session/close`, `session/delete`, `session/set_mode`, `session/set_config_option`, `session/prompt`, `session/request_permission`, `fs/read_text_file`, `fs/write_text_file`, all `terminal/*` methods, `elicitation/create`, `session/update`, `session/cancel`, `elicitation/complete` and `$/cancel_request`.

`rawInput`, `rawOutput`, elicitation schemas, select options and extension payloads remain `JsonElement`.

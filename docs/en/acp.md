# Capastrum - ACP

> [Русская версия ->](../ru/acp.md) - [README](https://github.com/jrfrigat/Capastrum/blob/main/README.md) - [Documentation](../README.md)

---

## Overview

| Package | Role |
| :-- | :-- |
| `Capastrum.Acp.Protocol` | ACP v1 contracts and source-generated serializer |
| `Capastrum.Acp` | Host: launches an agent, serves its requests, enforces permissions |
| `Capastrum.Acp.Server` | Agent: sessions, prompts, updates, outbound requests |

ACP connects an editor or another host to an agent over stdio. The host owns the process, the workspace and the security boundary; the agent asks for permission before it runs a command or writes a file. Capastrum implements both sides with the same typed contracts.

---

## Host

```csharp
await using var agent = await AcpAgentClient.StartAsync(new AcpClientOptions
{
    Executable = "npx",
    Arguments = ["-y", "@zed-industries/claude-code-acp"],
    WorkspaceRoot = "/path/to/project",
});
```

`StartAsync` launches the process, connects over newline-delimited stdio and performs `initialize`. Use `ConnectAsync` with an existing `IJsonRpcTransport` for tests, custom transports or an agent that is already running.

### Session lifecycle

```csharp
var session = await agent.NewSessionAsync("/path/to/project");

var response = await agent.SendPromptAsync(
    session.SessionId,
    [new AcpTextContent { Text = "Find outdated dependencies." }]);

await agent.CloseSessionAsync(session.SessionId);
```

The host also exposes `LoadSessionAsync`, `ResumeSessionAsync`, `ListSessionsAsync`, `DeleteSessionAsync`, `SetModeAsync` and `SetConfigOptionAsync`. `AuthenticateAsync` and `LogoutAsync` cover the authentication methods the agent advertises.

### Human-in-the-loop

`OnActionConfirm` is called for every `session/request_permission`:

```csharp
agent.OnActionConfirm((request, _) => ValueTask.FromResult(
    request.Command?.Contains("rm -rf", StringComparison.Ordinal) == true
        ? ConfirmationResult.Denied
        : ConfirmationResult.Allowed));
```

`AcpPermissionRequest` exposes the session, the tool call, its title, name, kind, raw input and the terminal command when one can be extracted. Return one of:

| Result | Outcome |
| :-- | :-- |
| `ConfirmationResult.Allowed` | Selects the first `allow_once` option; falls back to `cancelled` if none exists |
| `ConfirmationResult.Denied` | Selects the first `reject_once` option; falls back to `cancelled` if none exists |
| `ConfirmationResult.Selected(optionId)` | Selects that exact option |

If no handler is registered, every permission request is denied. An allow decision is never the default.

### File system

```csharp
agent.OnReadTextFile((request, _) => ValueTask.FromResult(
    new AcpReadTextFileResponse { Content = File.ReadAllText(request.Path) }));

agent.OnWriteTextFile((request, _) =>
{
    File.WriteAllText(request.Path, request.Content);
    return ValueTask.FromResult(new AcpWriteTextFileResponse());
});
```

Without the matching handler, the agent receives a JSON-RPC method-not-found error. The host decides which paths are legal; Capastrum does not add an implicit sandbox.

### Terminals

`AcpTerminalHost` starts processes requested by the agent, buffers their output up to the requested limit and implements `terminal/output`, `terminal/wait_for_exit`, `terminal/kill` and `terminal/release`. `AcpAgentClient` creates one by default and owns its lifetime. A custom host can be supplied through `AcpClientOptions.TerminalHost`.

### Updates and cancellation

```csharp
agent.OnSessionUpdate(update => Console.WriteLine(update.Text));
```

`session/update` notifications raise `SessionUpdated`; `update.Text` returns the text of content-chunk updates. `CancelAsync(sessionId)` sends `session/cancel`. Cancelling the token passed to `SendPromptAsync` does the same, and the agent is expected to answer with `stopReason=cancelled`.

---

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

`AgentInfo` and `PromptHandler` are required. Every other handler is optional; a request for an unconfigured method returns JSON-RPC method-not-found.

### Prompt context

`AcpPromptContext` is the agent's view of the host:

| Member | Purpose |
| :-- | :-- |
| `SendUpdateAsync` | Stream a `session/update` notification |
| `RequestPermissionAsync` | Ask the host to authorize a tool call |
| `ReadTextFileAsync` / `WriteTextFileAsync` | Access the workspace |
| `CreateTerminalAsync` / `GetTerminalOutputAsync` / `WaitForTerminalExitAsync` / `KillTerminalAsync` / `ReleaseTerminalAsync` | Run and manage terminals |
| `ElicitAsync` | Ask the user for structured input |

The permission response contains the host's selected option or a cancellation outcome. The agent must honour it before executing the tool call.

### Session handlers

`AcpAgentServerOptions` provides handlers for `Authenticate`, `Logout`, `NewSession`, `LoadSession`, `ResumeSession`, `ListSessions`, `CloseSession`, `DeleteSession`, `SetMode`, `SetConfigOption` and `Prompt`. State belongs to your application; the server routes requests and owns the protocol connection.

### Cancellation

The server observes `session/cancel`. The cancellation token passed to `PromptHandler` is cancelled when the host cancels the turn. A handler that cannot stop immediately should still return `AcpPromptResponse` with `StopReason = AcpStopReason.Cancelled`.

---

## Coverage

Requests:

`initialize`, `authenticate`, `logout`, `session/new`, `session/load`, `session/resume`, `session/list`, `session/close`, `session/delete`, `session/set_mode`, `session/set_config_option`, `session/prompt`, `session/request_permission`, `fs/read_text_file`, `fs/write_text_file`, `terminal/create`, `terminal/output`, `terminal/wait_for_exit`, `terminal/kill`, `terminal/release`, `elicitation/create`.

Notifications:

`session/update`, `session/cancel`, `elicitation/complete`.

`rawInput`, `rawOutput`, elicitation schemas, select options and extension payloads remain `JsonElement`.

---

## Security notes

- No permission handler means deny. Register one before starting a session in an untrusted workspace.
- File handlers are explicit. A missing handler is an error, not unrestricted access.
- Terminal processes run with the host's identity. Restrict the workspace and inspect permission requests before allowing them.
- `ConfirmationResult.Allowed` maps to `allow_once`; it does not grant a session-wide or permanent permission.

---

## Related guides

- [JSON-RPC core](json-rpc.md) - envelope, peer and transports
- [MCP](mcp.md) - tools, resources and prompts
- [Architecture](architecture.md) - package graph and design decisions

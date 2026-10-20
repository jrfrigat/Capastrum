# Capastrum

Capastrum is a .NET 10 toolkit for C# applications that need to talk to AI models and agents. It implements the Model Context Protocol (MCP) and the Agent Client Protocol (ACP) as strictly typed contracts over JSON-RPC 2.0, without imposing a framework, an agent runtime or a logging engine.

The name comes from Latin *capistrum* — a halter or a binding. The library binds an application to an AI model or an agent through one predictable protocol layer.

## Why Capastrum

- **Tool, not framework.** Use only the protocol pieces you need; the rest of the application stays yours.
- **Full protocol coverage.** MCP 2026-07-28 and ACP v1, both directions, with typed requests, results and notifications.
- **Security by default.** ACP permission requests can be inspected, allowed or denied before a terminal command or a file edit runs.
- **Built for performance.** `System.Text.Json` source generation, `System.IO.Pipelines` framing and allocation-conscious hot paths. No JSON.NET, no Newtonsoft.Json.
- **Independent packages.** Every package is published separately and carries only its own dependencies.

## Packages

| Package | Purpose |
| --- | --- |
| `Capastrum.JsonRpc` | JSON-RPC 2.0 peer, message model and transports (stdio, WebSocket, HTTP) |
| `Capastrum.Mcp.Protocol` | MCP 2026-07-28 contracts |
| `Capastrum.Mcp` | MCP server: tools, resources and prompts |
| `Capastrum.Mcp.Client` | MCP client for external servers |
| `Capastrum.Acp.Protocol` | ACP v1 contracts |
| `Capastrum.Acp` | ACP host: launch and orchestrate agents |
| `Capastrum.Acp.Server` | ACP agent: expose a C# application as an agent |

## Quick start

### MCP server

```csharp
using Capastrum.Mcp;

await using var server = new McpServer(new McpServerOptions
{
    Name = "Capastrum-Tools",
    Version = "1.0.0",
});

server.RegisterTool(
    "read_kanban_board",
    "Returns the active cards",
    async (request, cancellationToken) =>
    {
        var cards = await backlog.GetActiveCardsAsync(cancellationToken);
        return McpToolResult.Json(cards);
    });

await server.RunAsync(McpTransport.Stdio);
```

### MCP client

```csharp
using Capastrum.Mcp.Client;

await using var client = await McpClient.ConnectAsync(new StdioMcpClientOptions
{
    Command = "my-mcp-server",
    Arguments = [],
});

var tools = await client.ListToolsAsync();
var result = await client.CallToolAsync("read_kanban_board", arguments: null);
```

### ACP host

```csharp
using Capastrum.Acp;

await using var agent = new AcpAgentClient(new AcpClientOptions
{
    Executable = "npx",
    Arguments = ["@zed-industries/claude-code-acp"],
    WorkspaceRoot = "/path/to/project",
});

agent.OnSessionUpdate(update => Console.WriteLine(update.Text));
agent.OnActionConfirm(request =>
    request.Command.Contains("rm -rf", StringComparison.Ordinal)
        ? ConfirmationResult.Denied
        : ConfirmationResult.Allowed);

await agent.InitializeAsync();
await agent.SendPromptAsync("Find outdated dependencies and update them.");
```

## Requirements

- .NET 10 SDK (the repository pins `10.0.401` in `global.json`).
- No external dependencies in `Capastrum.JsonRpc`; the other packages reference only the protocol packages they expose.

## Build and test

```bash
dotnet build Capastrum.sln -c Release
dotnet test Capastrum.sln -c Release
dotnet pack Capastrum.sln -c Release -o artifacts/packages
pwsh ./eng/verify-packages.ps1
```

## Documentation

See [docs/index.md](docs/index.md) for package responsibilities, design decisions and guides.

## License

MIT

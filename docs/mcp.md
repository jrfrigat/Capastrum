# MCP

`Capastrum.Mcp.Protocol` contains the MCP `2026-07-28` contracts; `Capastrum.Mcp` is the server and `Capastrum.Mcp.Client` is the client. Both use the same `McpJsonContext` source-generated serializer.

## Server

```csharp
var server = new McpServer(new McpServerOptions
{
    Name = "Capastrum-Tools",
    Version = "1.0.0",
});

server.RegisterTool(
    "echo",
    "Echoes a value",
    (context, _) => ValueTask.FromResult<McpResult>(
        McpToolResult.Text(context.Arguments.GetProperty("value").GetString() ?? string.Empty)));

server.RegisterResource(
    new McpResource { Uri = "file:///readme.txt", Name = "readme" },
    (context, _) => ValueTask.FromResult(new McpReadResourceResult
    {
        Contents = new[] { new McpResourceContents { Uri = context.Uri, Text = "hello" } },
    }));

server.RegisterPrompt(
    new McpPrompt { Name = "greet" },
    (_, _) => ValueTask.FromResult(new McpGetPromptResult
    {
        Messages = new[]
        {
            new McpPromptMessage
            {
                Role = McpRole.User,
                Content = new McpTextContent { Text = "Say hello." },
            },
        },
    }));

await server.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());
```

Handlers receive `McpRequestMeta`, so progress tokens and client info are available. `context.ReportProgressAsync` and `context.LogAsync` send the corresponding notifications.

### MRTR

A tool handler can return input-required instead of a final result:

```csharp
return McpToolResult.InputRequired(
    "sampling-1",
    new McpCreateMessageInputRequest
    {
        Params = new McpCreateMessageRequestParams
        {
            MaxTokens = 100,
            Messages = new[] { new McpSamplingMessage { Role = McpRole.User, Content = JsonSerializer.SerializeToElement(prompt) } },
        },
    },
    requestState: "tool-state");
```

The client resolves the input and retries the call with `inputResponses`; the handler sees them on `context.InputResponses`.

### Subscriptions

`server.NotifyToolsChangedAsync()`, `NotifyResourcesChangedAsync()`, `NotifyPromptsChangedAsync()` and `NotifyResourceUpdatedAsync(uri)` send notifications only to subscriptions whose filters match.

### HTTP

`server.HandleHttpRequestAsync` accepts a transport-neutral `McpHttpRequest` and checks `Mcp-Method` and `Mcp-Name` before dispatching. It is suitable for an ASP.NET Core endpoint or any HTTP host; SSE subscriptions need a streaming adapter.

## Client

```csharp
await using var client = await McpClient.ConnectAsync(new StdioMcpClientOptions
{
    Command = "my-mcp-server",
});

var tools = await client.ListToolsAsync();
var result = await client.CallToolAsync("echo", JsonSerializer.SerializeToElement(new { value = "hello" }));
```

The client performs `server/discover` on connect and stores `ServerInfo`, `Capabilities` and `SupportedVersions`. It resolves MRTR input requests through the `SamplingHandler`, `RootsHandler` and `ElicitationHandler` options. `NotificationReceived`, `ToolsListChanged`, `ResourcesListChanged`, `PromptsListChanged` and `ResourceUpdated` expose incoming notifications.

HTTP clients use `HttpMcpClientOptions` and add the protocol headers automatically.

## Coverage

`server/discover`, `tools/list`, `tools/call`, `resources/list`, `resources/read`, `resources/templates/list`, `prompts/list`, `prompts/get`, `completion/complete`, `subscriptions/listen`, `sampling/createMessage`, `roots/list`, `elicitation/create`, and every list-changed, progress, message, resource-updated and subscription-acknowledged notification.

`arguments`, JSON schemas, structured content and elicitation schemas remain `JsonElement` because their shape belongs to the application.

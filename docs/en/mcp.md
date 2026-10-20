# Capastrum - MCP

> [Русская версия ->](../ru/mcp.md) - [README](https://github.com/jrfrigat/Capastrum/blob/main/README.md) - [Documentation](../README.md)

---

## Overview

| Package | Role |
| :-- | :-- |
| `Capastrum.Mcp.Protocol` | MCP 2026-07-28 contracts and source-generated serializer |
| `Capastrum.Mcp` | Server: tools, resources, prompts, completion, subscriptions, HTTP |
| `Capastrum.Mcp.Client` | Client: stdio, WebSocket, HTTP, MRTR resolution |

The protocol is stateless at its core. Every request carries its protocol version and client capabilities in `_meta`, servers advertise themselves through `server/discover`, and results declare their kind with `resultType`. Servers can be hosted as a stdio process, behind an HTTP endpoint, or over any duplex `IJsonRpcTransport`.

---

## Server

```csharp
var server = new McpServer(new McpServerOptions
{
    Name = "Capastrum-Tools",
    Version = "1.0.0",
});
```

### Tools

```csharp
server.RegisterTool(
    "echo",
    "Echoes a value",
    (context, _) => ValueTask.FromResult<McpResult>(
        McpToolResult.Text(context.Arguments.GetProperty("value").GetString() ?? string.Empty)));
```

`RegisterTool` accepts a full `McpTool` for explicit input and output schemas, or a name, description and handler with a generated permissive input schema. `McpToolResult` creates text, JSON, error and input-required results.

### Resources and templates

```csharp
server.RegisterResource(
    new McpResource { Uri = "file:///readme.txt", Name = "readme" },
    (context, _) => ValueTask.FromResult(new McpReadResourceResult
    {
        Contents = new[] { new McpResourceContents { Uri = context.Uri, Text = "hello" } },
    }));

server.RegisterResourceTemplate(
    new McpResourceTemplate { UriTemplate = "file:///{path}", Name = "files" },
    (context, _) => ReadTextResourceAsync(context.Uri));
```

Exact resources are matched before templates, and templates are matched in registration order.

### Prompts

```csharp
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
```

Register `SetCompletionHandler` to answer `completion/complete` for prompt arguments and resource templates.

### Request context

Tool handlers receive `McpToolCallContext` with the tool name, `Arguments`, `Meta`, `InputResponses` and `RequestState`. `Meta` exposes the progress token, protocol version and client information.

```csharp
await context.ReportProgressAsync(0.5, total: 1.0, message: "Halfway there");
await context.LogAsync(McpLoggingLevel.Info, "Indexing complete");
```

Resource and prompt handlers receive their own context types with the same metadata and MRTR state.

### Multi-round-trip requests

A tool can ask the client for input instead of returning a final result:

```csharp
return McpToolResult.InputRequired(
    "sampling-1",
    new McpCreateMessageInputRequest
    {
        Params = new McpCreateMessageRequestParams
        {
            MaxTokens = 100,
            Messages = new[]
            {
                new McpSamplingMessage
                {
                    Role = McpRole.User,
                    Content = JsonSerializer.SerializeToElement(prompt),
                },
            },
        },
    },
    requestState: "tool-state");
```

The result carries `resultType: "input_required"`, the input requests and opaque `requestState`. The client resolves the input and retries the call with `inputResponses` and the same state; the handler reads both from its context. Sampling, roots and elicitation are supported as input requests.

### Subscriptions

```csharp
await server.NotifyToolsChangedAsync();
await server.NotifyResourcesChangedAsync();
await server.NotifyPromptsChangedAsync();
await server.NotifyResourceUpdatedAsync("file:///readme.txt");
```

Notifications are delivered only to `subscriptions/listen` streams whose filters match the change. The server sends `notifications/subscriptions/acknowledged` when it accepts a subscription.

### HTTP

`HandleHttpRequestAsync` accepts a transport-neutral `McpHttpRequest` and returns `McpHttpResponse`:

```csharp
var response = await server.HandleHttpRequestAsync(
    new McpHttpRequest
    {
        Method = "POST",
        Headers = headers,
        Body = body,
    },
    cancellationToken);
```

The server validates `Mcp-Method` and `Mcp-Name` before dispatching. The response is suitable for an ASP.NET Core endpoint or any HTTP host. Long-lived subscriptions need a streaming adapter on top; `HandleHttpRequestAsync` handles single request/response exchanges.

---

## Client

```csharp
await using var client = await McpClient.ConnectAsync(new StdioMcpClientOptions
{
    Command = "my-mcp-server",
});

var tools = await client.ListToolsAsync();
var result = await client.CallToolAsync(
    "echo",
    JsonSerializer.SerializeToElement(new { value = "hello" }));
```

`ConnectAsync` has three entry points:

| Options | Transport |
| :-- | :-- |
| `StdioMcpClientOptions` | Launches a child process and uses newline-delimited stdio |
| `HttpMcpClientOptions` | POSTs to an HTTP endpoint and adds `Mcp-Method` / `Mcp-Name` headers |
| `IJsonRpcTransport` + `McpClientOptions` | Any custom or pre-connected transport, including `WebSocketJsonTransport` |

The client performs `server/discover` during connect and stores `ServerInfo`, `Capabilities` and `SupportedVersions`.

### Operations

| Method | Purpose |
| :-- | :-- |
| `DiscoverAsync` | Re-read server identity and capabilities |
| `ListToolsAsync` / `CallToolAsync` | Tools |
| `ListResourcesAsync` / `ReadResourceAsync` / `ListResourceTemplatesAsync` | Resources |
| `ListPromptsAsync` / `GetPromptAsync` | Prompts |
| `CompleteAsync` | Argument completion |
| `ListenAsync` | Open a subscription stream |

### MRTR handlers

Set `SamplingHandler`, `RootsHandler` and `ElicitationHandler` in the client options. When a tool returns `input_required`, the client invokes the matching handler, collects the responses under their input ids and retries the original call with `inputResponses` and `requestState`. The loop continues until the tool returns a complete result or the client has no handler for a requested input.

### Notifications

`NotificationReceived` exposes every raw notification. Typed events are available for the common cases:

- `ToolsListChanged`
- `ResourcesListChanged`
- `PromptsListChanged`
- `ResourceUpdated`
- `StandardErrorReceived` for a stdio child process

---

## Coverage

Requests:

`server/discover`, `tools/list`, `tools/call`, `resources/list`, `resources/read`, `resources/templates/list`, `prompts/list`, `prompts/get`, `completion/complete`, `subscriptions/listen`, `sampling/createMessage`, `roots/list`, `elicitation/create`.

Notifications:

`notifications/cancelled`, `notifications/progress`, `notifications/message`, `notifications/tools/list_changed`, `notifications/resources/list_changed`, `notifications/resources/updated`, `notifications/prompts/list_changed`, `notifications/subscriptions/acknowledged`.

`arguments`, JSON schemas, structured content and elicitation schemas remain `JsonElement` because their shape belongs to the application.

---

## Related guides

- [JSON-RPC core](json-rpc.md) - envelope, peer and transports
- [ACP](acp.md) - host and agent side
- [Architecture](architecture.md) - package graph and design decisions

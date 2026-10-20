# Capastrum - Architecture

> [Русская версия ->](../ru/architecture.md) - [README](https://github.com/jrfrigat/Capastrum/blob/main/README.md) - [Documentation](../README.md)

---

## Package graph

```text
Capastrum.JsonRpc
├── Capastrum.Mcp.Protocol ── Capastrum.Mcp
│                          └─ Capastrum.Mcp.Client
└── Capastrum.Acp.Protocol ── Capastrum.Acp
                           └─ Capastrum.Acp.Server
```

The graph is a tree with one shared root. A package references only what is directly below it, and MCP never references ACP or the other way around. An application can take one branch, both branches, or a single leaf without carrying the rest.

| Layer | Owns | Does not own |
| :-- | :-- | :-- |
| `Capastrum.JsonRpc` | Envelope, ids, request correlation, transports, stream framing | Protocol methods, schemas, process lifetime |
| `*.Protocol` | Typed records, enums, method and notification names, serializers | Transport, dispatch, application behavior |
| `Capastrum.Mcp` / `.Client` | MCP dispatch, registration, discovery, MRTR, subscriptions | Hosting, DI, logging, configuration |
| `Capastrum.Acp` / `.Server` | ACP host and agent behavior, permissions, files, terminals | Editor UI, agent logic, workspace policy |

---

## Protocol layering

### JSON-RPC

The core knows three message shapes and one peer. It correlates responses by `JsonRpcId`, dispatches incoming requests to a single handler and raises notifications to subscribers. Protocol-specific concerns stay out: cancellation notifications, error code ranges and parameter shapes belong to the layer above.

`IJsonRpcTransport` is deliberately small: `SendAsync` and `ReadAsync` over `JsonRpcMessage`. Newline, WebSocket and HTTP transports can therefore be swapped without touching peers or protocol code.

### MCP

MCP is a stateless request/response protocol with server-initiated input. The server is a registry of tools, resources, resource templates and prompts; each request is dispatched against that registry with a per-request context.

Key decisions:

- `server/discover` is the single place where a client learns the server identity, capabilities and supported versions.
- Protocol version and client capabilities travel in `_meta` on every request, so a server does not need a connection-scoped handshake.
- `resultType` distinguishes a complete result from an `input_required` result.
- MRTR state is opaque to the protocol layer. The server returns `requestState`; the client returns it unchanged with `inputResponses`.
- Subscriptions are filter objects attached to a `subscriptions/listen` stream; notifications are routed by those filters rather than broadcast to every connection.

The HTTP surface is transport-neutral. `HandleHttpRequestAsync` accepts an `McpHttpRequest` and returns an `McpHttpResponse`, so ASP.NET Core, a minimal API or a custom server can host it without Capastrum taking a dependency on a web framework.

### ACP

ACP is a host/agent protocol with a long-lived connection. The host launches the agent and answers its outbound requests: permissions, file reads and writes, terminals, elicitation.

Key decisions:

- The host owns the process. `AcpAgentClient.StartAsync` launches it, connects stdio and performs `initialize` before returning.
- Permission is deny-by-default. Without `OnActionConfirm`, every `session/request_permission` receives a denial.
- File system access is explicit. A missing read or write handler produces a method-not-found error rather than unrestricted access.
- Terminals are implemented by `AcpTerminalHost`, which owns process lifetime, buffered output and the output limit.
- Session state belongs to the application. The agent server routes requests and prompt turns; it does not impose a session store.
- Cancellation is cooperative: `session/cancel` cancels the prompt token, and the agent returns a `cancelled` stop reason.

---

## Contracts and serialization

Every wire type is a record or enum in a protocol package. `System.Text.Json` source generation produces the metadata for requests, results, notifications and the JSON-RPC envelope. No protocol path falls back to reflection-based serialization.

Application-defined data stays `JsonElement`:

- tool arguments and structured content;
- JSON schemas for tools and elicitation;
- ACP `rawInput` and `rawOutput`;
- extension payloads.

This is a deliberate boundary: the protocol can type what the specification types, and the application keeps ownership of the shapes only it understands.

The serializer contexts are public so callers can compose typed calls without building their own metadata:

```csharp
var result = await peer.InvokeAsync(
    McpProtocol.Methods.ListTools,
    parameters: null,
    McpJsonContext.Default.McpListToolsResult);
```

---

## Async and lifetime

- All I/O is asynchronous and accepts `CancellationToken`.
- `JsonRpcPeer` owns one read loop and one pending-request table.
- Servers and clients implement `IAsyncDisposable`; disposing fails pending calls and releases transports or child processes.
- A stdio client kills its child process tree when connection setup fails.
- Hosts do not create a DI container, a logger or a configuration system. Their options objects are plain classes with `init` properties.

---

## Testing

The repository has six test projects:

| Project | Focus |
| :-- | :-- |
| `Capastrum.JsonRpc.Tests` | Envelope, peer, transports, errors |
| `Capastrum.Mcp.Tests` | Server dispatch, registration, MRTR, HTTP |
| `Capastrum.Mcp.Client.Tests` | Discovery, calls, notifications, handlers |
| `Capastrum.Acp.Tests` | Host permissions, files, terminals, sessions |
| `Capastrum.Acp.Server.Tests` | Agent dispatch, prompt turns, cancellation |
| `Capastrum.Conformance.Tests` | Protocol surface and a cross-protocol end-to-end chain |

The conformance project checks that every declared method is implemented and that the ACP host -> agent -> MCP client -> MCP server path works as one system.

`benchmarks/Capastrum.Benchmarks` measures JSON-RPC envelope serialization, a full in-memory peer round trip and MCP tool dispatch.

---

## Constraints

- **.NET 10 first.** The repository targets `net10.0` and uses C# 13/14 features available there. Other target frameworks are acceptable only if they do not slow down the .NET 10 build.
- **No framework coupling.** Capastrum is a set of libraries, not an application framework. It does not require a specific host, DI container or logging abstraction.
- **Independent packages.** Each package has its own description, dependencies and NuGet identity; there is no umbrella package.
- **Warnings are errors.** The build enables nullable analysis, analyzers and documentation generation.
- **Public API documentation is mandatory.** Every public type and member carries a `<summary>`; comments explain decisions, not syntax.

---

## Related guides

- [JSON-RPC core](json-rpc.md) - envelope, peer and transports
- [MCP](mcp.md) - server and client
- [ACP](acp.md) - host and agent

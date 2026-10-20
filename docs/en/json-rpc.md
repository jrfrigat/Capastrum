# Capastrum - JSON-RPC Core

> [Русская версия ->](../ru/json-rpc.md) - [README](https://github.com/jrfrigat/Capastrum/blob/main/README.md) - [Documentation](../README.md)

---

## Overview

`Capastrum.JsonRpc` implements the JSON-RPC 2.0 envelope, request correlation, cancellation and transports shared by MCP and ACP. It depends only on the .NET 10 base class library and uses `System.Text.Json` for serialization.

The package does not know either protocol. It moves JSON-RPC messages between two peers and leaves method names, parameter shapes and result types to the layer above.

---

## Message model

| Type | Purpose |
| :-- | :-- |
| `JsonRpcRequest` | An `id`, a `method` and optional `params`; always expects a response |
| `JsonRpcNotification` | A `method` and optional `params`; never expects a response |
| `JsonRpcResponse` | The request `id` and either a `result` or an `error` |
| `JsonRpcError` | A numeric code, a message and optional `data` |
| `JsonRpcId` | A numeric or string request identifier; `Text` returns its wire form |

`params` and `result` remain `JsonElement` in the core. Protocol packages deserialize them into their own source-generated contracts, so the core never allocates an intermediate object graph.

---

## Peer

`JsonRpcPeer` owns one read loop, correlates responses with outgoing requests and dispatches incoming requests and notifications.

```csharp
await using var peer = new JsonRpcPeer(transport);

peer.RequestHandler = async (request, cancellationToken) =>
{
    var result = await HandleAsync(request.Method, request.Params, cancellationToken);
    return new JsonRpcResponse { Id = request.Id, Result = result };
};

peer.NotificationReceived += notification => Console.WriteLine(notification.Method);

var response = await peer.InvokeAsync("tools/list");
await peer.NotifyAsync("notifications/cancelled");
```

### Behavior

- `InvokeAsync` allocates a request id, sends the request and waits for the matching response.
- `RequestHandler` answers incoming requests. A thrown exception becomes a JSON-RPC error response with the exception message.
- `NotificationReceived` is raised for every incoming notification.
- A cancelled `InvokeAsync` stops waiting and removes the pending entry. The protocol layer decides whether to send a cancellation notification.
- `DisposeAsync` fails all pending requests and disposes the transport.

### Typed responses

Protocol packages pass source-generated metadata explicitly:

```csharp
var result = await peer.InvokeAsync(
    "tools/list",
    parameters: null,
    McpJsonContext.Default.McpListToolsResult);
```

The overload returns the typed result without reflection-based serialization.

---

## Transports

| Transport | Use |
| :-- | :-- |
| `NewlineJsonTransport` | stdio and other duplex streams; one JSON message per line |
| `WebSocketJsonTransport` | WebSocket text frames |
| `HttpJsonTransport` | HTTP POST with a JSON response |

All transports implement `IJsonRpcTransport` with `SendAsync` and `ReadAsync`. A transport owns framing, not protocol semantics.

`HttpJsonTransport` accepts a caller-owned `HttpClient` and exposes `ConfigureRequest`, which lets a protocol layer add headers to every outgoing request. A notification completes as soon as the endpoint accepts the POST; a request reads the JSON response body.

`NewlineJsonTransport` uses `System.IO.Pipelines` to frame the stream and processes a final message even when the stream ends without a trailing newline.

---

## Errors

`JsonRpcPeer` throws `JsonRpcException` for error responses. The exception exposes `Code`, `Message` and `Data`.

Standard codes are available in `JsonRpcErrorCode`:

| Constant | Code | Meaning |
| :-- | --: | :-- |
| `ParseError` | -32700 | Invalid JSON |
| `InvalidRequest` | -32600 | Valid JSON, invalid request |
| `MethodNotFound` | -32601 | Unknown or unavailable method |
| `InvalidParams` | -32602 | Invalid method parameters |
| `InternalError` | -32603 | Internal error |

Protocol-specific codes live in the MCP and ACP packages.

---

## Cancellation

Cancellation is cooperative:

- A cancelled `InvokeAsync` removes its pending response and stops waiting.
- The peer does not send `$/cancel_request` or `notifications/cancelled` on its own; the protocol layer knows which notification the other side expects.
- Disposing the peer fails every pending call with `ObjectDisposedException` instead of leaving it pending.

---

## Performance

- Source-generated serialization for the envelope and every protocol payload.
- `System.IO.Pipelines` for newline framing.
- One read loop per peer and a single write lock per transport.
- No reflection-based serializer fallback in protocol paths.
- Allocation budgets are tracked by `benchmarks/Capastrum.Benchmarks`, which measures envelope serialization and a full in-memory request/response round trip.

---

## Related guides

- [MCP](mcp.md) - tools, resources, prompts, MRTR and subscriptions
- [ACP](acp.md) - host, agent, permissions, files and terminals
- [Architecture](architecture.md) - package graph and design constraints

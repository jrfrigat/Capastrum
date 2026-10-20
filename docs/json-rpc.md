# JSON-RPC core

`Capastrum.JsonRpc` implements the JSON-RPC 2.0 envelope, request correlation and transports shared by MCP and ACP. It depends only on the .NET 10 base class library and uses `System.Text.Json`; JSON.NET and StreamJsonRpc are not part of the package.

## Messages

- `JsonRpcRequest` carries an `id`, a `method` and optional `params`.
- `JsonRpcNotification` carries a `method` and optional `params` and expects no response.
- `JsonRpcResponse` carries the request `id` and either a `result` or an `error`.
- `JsonRpcId` is a number or a string.
- `JsonRpcError` carries a code, a message and optional `data`.

`params` and `result` stay as `JsonElement`; protocol packages deserialize them into their own source-generated contracts.

## Peer

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

- `InvokeAsync` allocates a numeric id, sends the request and waits for the matching response.
- `RequestHandler` answers incoming requests; an exception becomes a JSON-RPC error.
- `NotificationReceived` is raised for every incoming notification.
- A cancelled `InvokeAsync` stops waiting and removes the pending entry; the protocol layer decides whether to send its own cancellation notification.
- `DisposeAsync` fails pending requests and disposes the transport.

## Transports

| Transport | Use |
| --- | --- |
| `NewlineJsonTransport` | stdio and other duplex streams; one JSON message per line |
| `WebSocketJsonTransport` | WebSocket text frames |
| `HttpJsonTransport` | HTTP POST with a JSON response |

All transports implement `IJsonRpcTransport` with `SendAsync` and `ReadAsync`.

## Errors

`JsonRpcPeer` throws `JsonRpcException` for error responses. The exception exposes `Code`, `Message` and `Data`. Standard codes are in `JsonRpcErrorCode`.

## Performance

- Source-generated serialization for the envelope.
- `System.IO.Pipelines` for stream framing.
- One read loop per peer and a single write lock per transport.
- `ArrayPool`-free hot path today; allocation budgets are tracked by the benchmarks in `benchmarks/Capastrum.Benchmarks`.

## Source-generated payloads

Protocol packages pass `JsonTypeInfo<T>` overloads:

```csharp
var result = await peer.InvokeAsync("tools/list", parameters: null, McpJsonContext.Default.ListToolsResult);
```

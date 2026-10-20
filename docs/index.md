# Capastrum documentation

## Packages

| Package | Responsibility |
| --- | --- |
| `Capastrum.JsonRpc` | JSON-RPC 2.0 peer, message model and transports |
| `Capastrum.Mcp.Protocol` | MCP 2026-07-28 contracts |
| `Capastrum.Mcp` | MCP server |
| `Capastrum.Mcp.Client` | MCP client |
| `Capastrum.Acp.Protocol` | ACP v1 contracts |
| `Capastrum.Acp` | ACP host |
| `Capastrum.Acp.Server` | ACP agent server |

## Design

- Wire format: JSON-RPC 2.0 over newline-delimited JSON, WebSocket frames or HTTP.
- Serialization: `System.Text.Json` with source-generated contexts; JSON.NET is not used.
- Transports: `System.IO.Pipelines` for stream framing.
- Protocol versions: MCP `2026-07-28`, ACP `1`.

## Guides

- JSON-RPC core — [json-rpc.md](json-rpc.md)
- MCP server and client — [mcp.md](mcp.md)
- ACP host and agent — [acp.md](acp.md)

## Contributing

```bash
dotnet build Capastrum.sln -c Release
dotnet test Capastrum.sln -c Release
dotnet pack Capastrum.sln -c Release -o artifacts/packages
```

Public types and members carry `<summary>` documentation. Comments explain non-obvious decisions only.

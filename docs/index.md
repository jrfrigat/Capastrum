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
- Serialization: `System.Text.Json` with source-generated contexts.
- Transports: `System.IO.Pipelines` for stream framing.
- Protocol versions: MCP `2026-07-28`, ACP `1`.

## Guides

- MCP server quick start — planned.
- MCP client quick start — planned.
- ACP host quick start — planned.
- ACP agent quick start — planned.

## Contributing

```bash
dotnet build Capastrum.sln -c Release
dotnet test Capastrum.sln -c Release
dotnet pack Capastrum.sln -c Release -o artifacts/packages
```

Public types and members carry `<summary>` documentation. Comments explain non-obvious decisions only.

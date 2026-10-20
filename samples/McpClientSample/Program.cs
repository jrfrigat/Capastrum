using System.Text.Json;
using Capastrum.Mcp.Client;

var command = args.Length > 0 ? args[0] : "npx";
var arguments = args.Length > 1
    ? args[1..]
    : new[] { "-y", "@modelcontextprotocol/server-everything" };

await using var client = await McpClient.ConnectAsync(new StdioMcpClientOptions
{
    Command = command,
    Arguments = arguments,
});

var tools = await client.ListToolsAsync();
Console.WriteLine($"Connected to {client.ServerInfo?.Name} {client.ServerInfo?.Version}");

foreach (var tool in tools.Tools)
{
    Console.WriteLine($"- {tool.Name}: {tool.Description}");
}

if (tools.Tools.Count > 0)
{
    var first = tools.Tools[0];
    var result = await client.CallToolAsync(
        first.Name,
        first.InputSchema.ValueKind == JsonValueKind.Object
            ? JsonSerializer.SerializeToElement(new { })
            : null);

    Console.WriteLine($"Called {first.Name}: {result.IsError == true}");
}

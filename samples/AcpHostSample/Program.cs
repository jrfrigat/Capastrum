using Capastrum.Acp;
using Capastrum.Acp.Protocol;

var executable = args.Length > 0 ? args[0] : "npx";
var arguments = args.Length > 1
    ? args[1..]
    : new[] { "-y", "@zed-industries/claude-code-acp" };
var workspace = Directory.GetCurrentDirectory();

await using var agent = await AcpAgentClient.StartAsync(new AcpClientOptions
{
    Executable = executable,
    Arguments = arguments,
    WorkspaceRoot = workspace,
});

agent.OnSessionUpdate(update => Console.WriteLine(update.Text));
agent.OnActionConfirm((permission, _) => ValueTask.FromResult(
    permission.Command?.Contains("rm -rf", StringComparison.Ordinal) == true
        ? ConfirmationResult.Denied
        : ConfirmationResult.Allowed));

var session = await agent.NewSessionAsync(workspace);
var response = await agent.SendPromptAsync(
    session.SessionId,
    new[] { new AcpTextContent { Text = "List the files in this workspace." } });

Console.WriteLine($"Stopped: {response.StopReason}");

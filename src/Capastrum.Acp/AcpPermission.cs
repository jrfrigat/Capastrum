using System.Text.Json;
using Capastrum.Acp.Protocol;

namespace Capastrum.Acp;

/// <summary>
/// A permission request from the agent.
/// </summary>
public sealed class AcpPermissionRequest
{
    internal AcpPermissionRequest(AcpRequestPermissionRequest request)
    {
        SessionId = request.SessionId;
        ToolCall = request.ToolCall;
        Options = request.Options;
    }

    /// <summary>The session identifier.</summary>
    public string SessionId { get; }

    /// <summary>The tool call being authorized.</summary>
    public AcpToolCallUpdate ToolCall { get; }

    /// <summary>The options the host can choose from.</summary>
    public IReadOnlyList<AcpPermissionOption> Options { get; }

    /// <summary>The tool call title.</summary>
    public string? Title => ToolCall.Title;

    /// <summary>The tool name.</summary>
    public string? ToolName => ToolCall.Name;

    /// <summary>The tool kind.</summary>
    public AcpToolKind? Kind => ToolCall.Kind;

    /// <summary>The raw tool input.</summary>
    public JsonElement? RawInput => ToolCall.RawInput;

    /// <summary>The terminal command, when the tool input contains one.</summary>
    public string? Command => RawInput is { ValueKind: JsonValueKind.Object } input
        && input.TryGetProperty("command", out var command)
        && command.ValueKind == JsonValueKind.String
            ? command.GetString()
            : null;
}

/// <summary>
/// The host's decision for a permission request.
/// </summary>
public sealed class ConfirmationResult
{
    private ConfirmationResult(bool allowed, string? optionId)
    {
        IsAllowed = allowed;
        OptionId = optionId;
    }

    /// <summary>Allows the action.</summary>
    public static ConfirmationResult Allowed { get; } = new(true, null);

    /// <summary>Denies the action.</summary>
    public static ConfirmationResult Denied { get; } = new(false, null);

    /// <summary>Selects a specific permission option.</summary>
    public static ConfirmationResult Selected(string optionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(optionId);

        return new ConfirmationResult(true, optionId);
    }

    /// <summary>Whether the action is allowed.</summary>
    public bool IsAllowed { get; }

    /// <summary>The selected option identifier, when one was chosen.</summary>
    public string? OptionId { get; }

    internal AcpRequestPermissionOutcome ToOutcome(IReadOnlyList<AcpPermissionOption> options)
    {
        if (OptionId is { } optionId)
        {
            return new AcpSelectedPermissionOutcome { OptionId = optionId };
        }

        var kind = IsAllowed ? AcpPermissionOptionKind.AllowOnce : AcpPermissionOptionKind.RejectOnce;
        var option = options.FirstOrDefault(option => option.Kind == kind);

        return option is null
            ? new AcpCancelledPermissionOutcome()
            : new AcpSelectedPermissionOutcome { OptionId = option.OptionId };
    }
}

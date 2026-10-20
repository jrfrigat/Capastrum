using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Capastrum.Acp.Protocol;

namespace Capastrum.Acp;

/// <summary>
/// Runs terminals requested by an agent and buffers their output.
/// </summary>
public sealed class AcpTerminalHost : IAsyncDisposable, IDisposable
{
    private sealed class TerminalSession
    {
        public required Process Process { get; init; }
        public required StringBuilder Output { get; init; }
        public required TaskCompletionSource<AcpTerminalExitStatus?> Exit { get; init; }
        public required object Sync { get; init; }
        public required int? OutputByteLimit { get; init; }
        public bool Truncated { get; set; }
    }

    private readonly ConcurrentDictionary<string, TerminalSession> _terminals = new(StringComparer.Ordinal);
    private long _nextId;
    private bool _disposed;

    /// <summary>Starts a terminal for the given request.</summary>
    public ValueTask<AcpCreateTerminalResponse> CreateAsync(
        AcpCreateTerminalRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo(request.Command)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (var argument in request.Args ?? [])
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (!string.IsNullOrEmpty(request.Cwd))
        {
            startInfo.WorkingDirectory = request.Cwd;
        }

        if (request.Environment is not null)
        {
            foreach (var variable in request.Environment)
            {
                startInfo.Environment[variable.Name] = variable.Value;
            }
        }

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{request.Command}'.");

        var id = $"terminal-{Interlocked.Increment(ref _nextId)}";
        var session = new TerminalSession
        {
            Process = process,
            Output = new StringBuilder(),
            Exit = new TaskCompletionSource<AcpTerminalExitStatus?>(
                TaskCreationOptions.RunContinuationsAsynchronously),
            Sync = new object(),
            OutputByteLimit = request.OutputByteLimit,
        };

        _terminals[id] = session;
        _ = Task.Run(() => PumpAsync(session), CancellationToken.None);

        return ValueTask.FromResult(new AcpCreateTerminalResponse { TerminalId = id });
    }

    /// <summary>Returns the buffered output of a terminal.</summary>
    public ValueTask<AcpTerminalOutputResponse> GetOutputAsync(
        AcpTerminalOutputRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_terminals.TryGetValue(request.TerminalId, out var session))
        {
            throw new KeyNotFoundException($"Unknown terminal '{request.TerminalId}'.");
        }

        string output;
        bool truncated;

        lock (session.Sync)
        {
            output = session.Output.ToString();
            truncated = session.Truncated;
        }

        AcpTerminalExitStatus? exitStatus = null;
        if (session.Process.HasExited)
        {
            exitStatus = new AcpTerminalExitStatus { ExitCode = session.Process.ExitCode };
        }

        return ValueTask.FromResult(new AcpTerminalOutputResponse
        {
            Output = output,
            Truncated = truncated,
            ExitStatus = exitStatus,
        });
    }

    /// <summary>Waits for a terminal to exit.</summary>
    public async ValueTask<AcpWaitForTerminalExitResponse> WaitForExitAsync(
        AcpWaitForTerminalExitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_terminals.TryGetValue(request.TerminalId, out var session))
        {
            throw new KeyNotFoundException($"Unknown terminal '{request.TerminalId}'.");
        }

        var exitStatus = await session.Exit.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        return new AcpWaitForTerminalExitResponse { ExitStatus = exitStatus };
    }

    /// <summary>Kills a terminal.</summary>
    public ValueTask<AcpKillTerminalResponse> KillAsync(
        AcpKillTerminalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_terminals.TryGetValue(request.TerminalId, out var session))
        {
            throw new KeyNotFoundException($"Unknown terminal '{request.TerminalId}'.");
        }

        TryKill(session.Process);

        return ValueTask.FromResult(new AcpKillTerminalResponse());
    }

    /// <summary>Releases a terminal and its resources.</summary>
    public ValueTask<AcpReleaseTerminalResponse> ReleaseAsync(
        AcpReleaseTerminalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_terminals.TryRemove(request.TerminalId, out var session))
        {
            TryKill(session.Process);
            session.Process.Dispose();
        }

        return ValueTask.FromResult(new AcpReleaseTerminalResponse());
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var terminal in _terminals)
        {
            TryKill(terminal.Value.Process);
            terminal.Value.Process.Dispose();
        }

        _terminals.Clear();
        await ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private static async Task PumpAsync(TerminalSession session)
    {
        var outputTask = PumpStreamAsync(session, session.Process.StandardOutput, appendNewLine: true);
        var errorTask = PumpStreamAsync(session, session.Process.StandardError, appendNewLine: true);

        try
        {
            await session.Process.WaitForExitAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
        }

        await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);

        session.Exit.TrySetResult(new AcpTerminalExitStatus
        {
            ExitCode = TryGetExitCode(session.Process),
        });
    }

    private static async Task PumpStreamAsync(TerminalSession session, StreamReader reader, bool appendNewLine)
    {
        try
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                lock (session.Sync)
                {
                    session.Output.Append(line);

                    if (appendNewLine)
                    {
                        session.Output.AppendLine();
                    }

                    if (session.OutputByteLimit is { } limit && session.Output.Length > limit)
                    {
                        session.Output.Remove(0, session.Output.Length - limit);
                        session.Truncated = true;
                    }
                }
            }
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static int? TryGetExitCode(Process process)
    {
        try
        {
            return process.HasExited ? process.ExitCode : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }
}

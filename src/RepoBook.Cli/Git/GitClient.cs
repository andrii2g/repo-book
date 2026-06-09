using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace RepoBook.Git;

public sealed class GitClient
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    public async Task<string> RunAsync(string repositoryPath, params string[] args)
    {
        var stdout = await RunInternalAsync(repositoryPath, CancellationToken.None, args);
        return stdout.TrimEnd();
    }

    public async Task<IReadOnlyList<string>> RunLinesAsync(string repositoryPath, params string[] args)
    {
        var stdout = await RunInternalAsync(repositoryPath, CancellationToken.None, args);
        return stdout
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);
    }

    private static async Task<string> RunInternalAsync(
        string repositoryPath,
        CancellationToken cancellationToken,
        params string[] args)
    {
        using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(repositoryPath);

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        try
        {
            using var process = new Process { StartInfo = startInfo };

            if (!process.Start())
            {
                throw new GitCommandException("Failed to start git process.");
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                var message = string.IsNullOrWhiteSpace(stderr)
                    ? "git command failed."
                    : stderr.Trim();
                throw new GitCommandException(message);
            }

            return stdout;
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
        {
            throw new GitCommandException("git command timed out.", ex);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            throw new GitCommandException("git executable was not found on PATH.", ex);
        }
    }
}

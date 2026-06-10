using System.Diagnostics;

namespace ProjectManagerWeb.src.Services;

public class GitCommandRunner : IGitCommandRunner
{
    public async Task<ComandoResultado> RunAsync(string command, string? sshKey = null)
    {
        var useShell = OperatingSystem.IsWindows();

        var psi = new ProcessStartInfo
        {
            FileName = useShell ? "git" : "bash",
            Arguments = useShell ? command["git ".Length..] : $"-c \"{command.Replace("\"", "\\\"")}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(sshKey))
            psi.EnvironmentVariables["GIT_SSH_COMMAND"] = $"ssh -i \"{sshKey}\"";

        if (!useShell)
        {
            var sshSock = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
            if (!string.IsNullOrEmpty(sshSock))
                psi.EnvironmentVariables["SSH_AUTH_SOCK"] = sshSock;

            var sshPid = Environment.GetEnvironmentVariable("SSH_AGENT_PID");
            if (!string.IsNullOrEmpty(sshPid))
                psi.EnvironmentVariables["SSH_AGENT_PID"] = sshPid;
        }

        using var processo = Process.Start(psi);
        if (processo is null) return new ComandoResultado(string.Empty, "Processo não iniciado", -1);

        var output = await processo.StandardOutput.ReadToEndAsync();
        var error = await processo.StandardError.ReadToEndAsync();
        await processo.WaitForExitAsync();

        return new ComandoResultado(output, FiltrarWarnings(error), processo.ExitCode);
    }

    internal static string FiltrarWarnings(string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr)) return string.Empty;

        var linhas = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var semWarnings = linhas.Where(l => !l.StartsWith("Environment variable $"));
        return string.Join('\n', semWarnings);
    }
}

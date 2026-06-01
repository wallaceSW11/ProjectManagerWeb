using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils;

public class WindowsShellProvider : IShellProvider
{
    public void ExecutarComInterface(string command, string? perfilTerminal = null)
    {
        ProcessStartInfo psi;
        string executavel;

        if (!string.IsNullOrWhiteSpace(perfilTerminal))
        {
            var encodedCommand = Convert.ToBase64String(
                System.Text.Encoding.Unicode.GetBytes(command));

            executavel = "wt.exe";
            psi = new ProcessStartInfo
            {
                FileName = executavel,
                Arguments = $"-w 0 new-tab -p \"{perfilTerminal}\" pwsh -NoExit -EncodedCommand {encodedCommand}",
                UseShellExecute = true
            };
        }
        else
        {
            executavel = "pwsh.exe";
            psi = new ProcessStartInfo
            {
                FileName = executavel,
                Arguments = $"-NoExit -Command \"{command}\"",
                UseShellExecute = true
            };
        }

        Process.Start(psi);
    }

    public void ExecutarSemInterface(string command)
    {
        var encodedCommand = Convert.ToBase64String(
            System.Text.Encoding.Unicode.GetBytes(command));

        var psi = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            Arguments = $"-WindowStyle Hidden -EncodedCommand {encodedCommand}",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(psi);
    }

    public void ExecutarComoAdministrador(string command, string? perfilTerminal = null)
    {
        ProcessStartInfo psi;
        string executavel;

        if (!string.IsNullOrWhiteSpace(perfilTerminal))
        {
            var encodedCommand = Convert.ToBase64String(
                System.Text.Encoding.Unicode.GetBytes(command));

            executavel = "wt.exe";
            psi = new ProcessStartInfo
            {
                FileName = executavel,
                Arguments = $"-w 0 new-tab -p \"{perfilTerminal}\" pwsh -NoExit -EncodedCommand {encodedCommand}",
                UseShellExecute = true
            };
        }
        else
        {
            executavel = "pwsh.exe";
            psi = new ProcessStartInfo
            {
                FileName = executavel,
                Arguments = $"-NoExit -Command \"{command}\"",
                UseShellExecute = true,
                Verb = "runas"
            };
        }

        Process.Start(psi);
    }

    public List<string> ObterPerfisTerminal()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wt.exe",
                Arguments = "--list-profiles",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null) return [];

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            return output
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}

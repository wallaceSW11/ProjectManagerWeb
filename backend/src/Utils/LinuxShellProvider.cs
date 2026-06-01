using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils;

public class LinuxShellProvider : IShellProvider
{
    public void ExecutarComInterface(string command, string? perfilTerminal = null)
    {
        var args = BuildGnomeTerminalArgs(command, perfilTerminal);

        var psi = new ProcessStartInfo
        {
            FileName = "gnome-terminal",
            Arguments = args,
            UseShellExecute = false
        };

        Process.Start(psi);
    }

    public void ExecutarSemInterface(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{EscapeBash(command)}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process.Start(psi);
    }

    public void ExecutarComoAdministrador(string command, string? perfilTerminal = null)
    {
        ExecutarComInterface($"sudo {command}", perfilTerminal);
    }

    public List<string> ObterPerfisTerminal()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dconf",
                Arguments = "list /org/gnome/terminal/legacy/profiles:/",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null) return [];

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            var profileIds = output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => l.StartsWith(':') && l.EndsWith('/'))
                .Select(l => l.Trim(':', '/'))
                .ToList();

            var perfis = new List<string>();

            foreach (var id in profileIds)
            {
                var namePsi = new ProcessStartInfo
                {
                    FileName = "dconf",
                    Arguments = $"read /org/gnome/terminal/legacy/profiles:/:{id}/visible-name",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var nameProcess = Process.Start(namePsi);
                if (nameProcess is null) continue;

                var name = nameProcess.StandardOutput.ReadToEnd().Trim().Trim('\'');
                nameProcess.WaitForExit(3000);

                if (!string.IsNullOrWhiteSpace(name))
                    perfis.Add(name);
            }

            return perfis;
        }
        catch
        {
            return [];
        }
    }

    private static string BuildGnomeTerminalArgs(string command, string? perfilTerminal)
    {
        var profile = !string.IsNullOrWhiteSpace(perfilTerminal)
            ? $"--profile=\"{perfilTerminal}\" "
            : "";

        return $"--tab {profile}-- bash -c \"{EscapeBash(command)}; exec bash\"";
    }

    private static string EscapeBash(string command) =>
        command.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

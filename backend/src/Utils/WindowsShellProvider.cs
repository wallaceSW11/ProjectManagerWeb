using System.Diagnostics;
using System.Text.Json;

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
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var possiblePaths = new[]
            {
                Path.Combine(localAppData, "Packages", "Microsoft.WindowsTerminal_8wekyb3d8bbwe", "LocalState", "settings.json"),
                Path.Combine(localAppData, "Packages", "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe", "LocalState", "settings.json"),
                Path.Combine(localAppData, "Microsoft", "Windows Terminal", "settings.json")
            };

            foreach (var settingsPath in possiblePaths)
            {
                if (!File.Exists(settingsPath)) continue;

                var json = File.ReadAllText(settingsPath);
                if (string.IsNullOrWhiteSpace(json)) continue;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("profiles", out var profiles)) continue;
                if (!profiles.TryGetProperty("list", out var list)) continue;

                var nomes = new List<string>();
                foreach (var item in list.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var nameProp))
                    {
                        var nome = nameProp.GetString();
                        if (!string.IsNullOrWhiteSpace(nome))
                            nomes.Add(nome);
                    }
                }

                if (nomes.Count > 0) return nomes;
            }

            return [];
        }
        catch
        {
            return [];
        }
    }
}

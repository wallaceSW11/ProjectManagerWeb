using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils.Terminais;

public class GhosttyTerminal : ITerminalEmulator
{
    public void Executar(string command, string? perfilTerminal = null, string? workingDirectory = null)
    {
        var trimmed = command.TrimEnd(' ', ';');
        var execBash = !string.IsNullOrWhiteSpace(workingDirectory)
            ? $"cd '{EscapeBash(workingDirectory)}' && exec bash"
            : "exec bash";

        Process.Start(new ProcessStartInfo
        {
            FileName = "ghostty",
            Arguments = $"-e bash -l -i -c \"trap '' INT; (trap - INT; {EscapeBash(trimmed)}); {execBash}\"",
            UseShellExecute = false
        });
    }

    public List<string> ObterPerfis()
    {
        // Ghostty não suporta perfis via CLI
        return [];
    }

    private static string EscapeBash(string command) =>
        command.Replace("\"", "\\\"");
}

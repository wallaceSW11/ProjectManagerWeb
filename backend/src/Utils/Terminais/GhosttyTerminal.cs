using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils.Terminais;

public class GhosttyTerminal : ITerminalEmulator
{
    public void Executar(string command, string? perfilTerminal = null)
    {
        var trimmed = command.TrimEnd(' ', ';');

        Process.Start(new ProcessStartInfo
        {
            FileName = "ghostty",
            Arguments = $"-e bash -l -c \"{EscapeBash(trimmed)}; exec bash\"",
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

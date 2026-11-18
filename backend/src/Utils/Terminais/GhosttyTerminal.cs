using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils.Terminais;

public class GhosttyTerminal : ITerminalEmulator
{
    public void Executar(string command, string? perfilTerminal = null, string? workingDirectory = null, string? complementoTerminal = null)
    {
        var trimmed = command.TrimEnd(' ', ';');
        var execShell = !string.IsNullOrWhiteSpace(workingDirectory)
            ? $"cd '{EscapeBash(workingDirectory)}' && stty sane && exec \"$SHELL\""
            : "stty sane && exec \"$SHELL\"";
        var complemento = string.IsNullOrWhiteSpace(complementoTerminal) ? "" : $"{complementoTerminal.Trim()} ";

        Process.Start(new ProcessStartInfo
        {
            FileName = "ghostty",
            Arguments = $"{complemento}-e bash -l -i -c \"trap '' INT; (trap - INT; {EscapeBash(trimmed)}); {execShell}\"",
            UseShellExecute = false
        });
    }

    public List<string> ObterPerfis()
    {
        return [];
    }

    private static string EscapeBash(string command) =>
        command.Replace("\"", "\\\"");
}

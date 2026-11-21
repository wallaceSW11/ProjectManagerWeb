using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils.Terminais;

public class GhosttyTerminal : ITerminalEmulator
{
    public void Executar(string command, string shell, string? perfilTerminal = null, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ghostty",
            UseShellExecute = false
        };

        psi.ArgumentList.Add("-e");
        ScriptTerminalPosix.AdicionarShell(psi, shell, command, workingDirectory);

        Process.Start(psi);
    }

    public List<string> ObterPerfis()
    {
        return [];
    }
}

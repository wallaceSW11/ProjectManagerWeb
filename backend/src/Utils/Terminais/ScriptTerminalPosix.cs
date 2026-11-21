using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils.Terminais;

internal static class ScriptTerminalPosix
{
    internal static void AdicionarComplemento(ProcessStartInfo psi, string? complementoTerminal)
    {
        foreach (var argumento in ShellPosix.SepararArgumentos(complementoTerminal))
            psi.ArgumentList.Add(argumento);
    }

    internal static void AdicionarShell(ProcessStartInfo psi, string shell, string command, string? workingDirectory)
    {
        var comando = command.TrimEnd(' ', ';');
        var reabrirShell = string.IsNullOrWhiteSpace(workingDirectory)
            ? $"stty sane && exec {shell}"
            : $"cd {ShellPosix.Citar(workingDirectory)} && stty sane && exec {shell}";

        psi.ArgumentList.Add(shell);
        psi.ArgumentList.Add("-l");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add($"trap '' INT; (trap - INT; {comando}); {reabrirShell}");

        AmbienteGrafico.Aplicar(psi);
    }
}

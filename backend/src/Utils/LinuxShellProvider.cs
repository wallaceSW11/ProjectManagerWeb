using System.Diagnostics;
using ProjectManagerWeb.src.Services;
using ProjectManagerWeb.src.Utils.Terminais;

namespace ProjectManagerWeb.src.Utils;

public class LinuxShellProvider(ConfiguracaoService configuracaoService) : IShellProvider
{
    private ITerminalEmulator? _terminal;

    private ITerminalEmulator Terminal =>
        _terminal ??= ResolverTerminal();

    public void ExecutarComInterface(string command, string? perfilTerminal = null)
    {
        Terminal.Executar(command, perfilTerminal);
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

    public List<string> ObterPerfisTerminal() => Terminal.ObterPerfis();

    public void RecarregarTerminal() => _terminal = null;

    private ITerminalEmulator ResolverTerminal()
    {
        var config = configuracaoService.ObterConfiguracaoAsync().GetAwaiter().GetResult();
        return TerminalEmulatorFactory.Criar(config.TerminalLinux);
    }

    private static string EscapeBash(string command) =>
        command.Replace("\"", "\\\"");
}

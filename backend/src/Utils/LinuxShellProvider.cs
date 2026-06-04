using System.Diagnostics;
using ProjectManagerWeb.src.Services;
using ProjectManagerWeb.src.Utils.Terminais;

namespace ProjectManagerWeb.src.Utils;

public class LinuxShellProvider(ConfiguracaoService configuracaoService) : IShellProvider
{
    private ITerminalEmulator? _terminal;

    private ITerminalEmulator Terminal =>
        _terminal ??= ResolverTerminal();

    private static string GarantirPathUsuario(string command)
    {
        var prefix = "export PATH=\"$HOME/.opencode/bin:$HOME/.local/share/pnpm/bin:$HOME/.local/bin:$HOME/bin:$PATH\"; ";
        return prefix + command;
    }

    private static string GarantirAmbienteGrafico(string command)
    {
        var display = Environment.GetEnvironmentVariable("DISPLAY");
        var xauthority = Environment.GetEnvironmentVariable("XAUTHORITY");
        var wayland = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");

        if (string.IsNullOrEmpty(display) || string.IsNullOrEmpty(xauthority))
        {
            var envSystemd = ExecutarProcesso("systemctl", "--user show-environment");
            if (!string.IsNullOrEmpty(envSystemd))
            {
                foreach (var linha in envSystemd.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (string.IsNullOrEmpty(display) && linha.StartsWith("DISPLAY="))
                        display = linha["DISPLAY=".Length..].Trim();
                    if (string.IsNullOrEmpty(xauthority) && linha.StartsWith("XAUTHORITY="))
                        xauthority = linha["XAUTHORITY=".Length..].Trim();
                    if (string.IsNullOrEmpty(wayland) && linha.StartsWith("WAYLAND_DISPLAY="))
                        wayland = linha["WAYLAND_DISPLAY=".Length..].Trim();
                }
            }
        }

        var exports = "";
        if (!string.IsNullOrEmpty(display))
            exports += $"export DISPLAY=\"{EscapeBash(display)}\"; ";
        if (!string.IsNullOrEmpty(xauthority))
            exports += $"export XAUTHORITY=\"{EscapeBash(xauthority)}\"; ";
        if (!string.IsNullOrEmpty(wayland))
            exports += $"export WAYLAND_DISPLAY=\"{EscapeBash(wayland)}\"; ";

        return exports + command;
    }

    private static string ExecutarProcesso(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null) return string.Empty;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return output;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string RemoverMarcadorExit(string command)
    {
        var trimmed = command.TrimEnd();
        if (trimmed.EndsWith("Exit;", StringComparison.OrdinalIgnoreCase))
            return trimmed[..^5].TrimEnd();
        return trimmed;
    }

    public void ExecutarComInterface(string command, string? perfilTerminal = null)
    {
        Terminal.Executar(GarantirPathUsuario(command), perfilTerminal);
    }

    public void ExecutarSemInterface(string command)
    {
        var comandoLimpo = RemoverMarcadorExit(GarantirAmbienteGrafico(GarantirPathUsuario(command)));

        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-l -c \"{EscapeBash(comandoLimpo)}\"",
            UseShellExecute = false,
            CreateNoWindow = true
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

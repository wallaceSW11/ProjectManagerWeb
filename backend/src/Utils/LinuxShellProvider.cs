using System.Diagnostics;
using System.Text.RegularExpressions;
using ProjectManagerWeb.src.Services;
using ProjectManagerWeb.src.Utils.Terminais;

namespace ProjectManagerWeb.src.Utils;

public class LinuxShellProvider(ConfiguracaoService configuracaoService) : IShellProvider
{
    private static readonly string[] GerenciadoresArquivos = ["nautilus", "dolphin", "nemo", "thunar", "pcmanfm"];
    private static readonly string[] EditoresTexto = ["code", "gnome-text-editor", "gedit", "kate", "xed", "mousepad", "sublime_text"];

    private ITerminalEmulator? _terminal;

    private ITerminalEmulator Terminal =>
        _terminal ??= ResolverTerminal();

    private static string GarantirPathUsuario(string command) =>
        $"export PATH=\"$HOME/.opencode/bin:$HOME/.local/share/pnpm/bin:$HOME/.local/bin:$HOME/bin:$PATH\"; {command}";

    internal static string RemoverMarcadorExit(string command)
    {
        var trimmed = command.TrimEnd();
        if (trimmed.EndsWith("Exit;", StringComparison.OrdinalIgnoreCase))
            return trimmed[..^5].TrimEnd();
        return trimmed;
    }

    public void ExecutarComInterface(string command, string? perfilTerminal = null, string? complementoTerminal = null) =>
        Terminal.Executar(GarantirPathUsuario(command), ShellPosix.Resolver(), perfilTerminal, ExtrairDiretorioDoComando(command), complementoTerminal);

    public void ExecutarSemInterface(string command)
    {
        var comandoLimpo = RemoverMarcadorExit(GarantirPathUsuario(command));

        var psi = new ProcessStartInfo
        {
            FileName = ShellPosix.Resolver(),
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("-l");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(comandoLimpo);

        AmbienteGrafico.Aplicar(psi);

        var processo = Process.Start(psi);
        if (processo is null) return;

        _ = RegistrarResultado(processo, command);
    }

    public void ExecutarComoAdministrador(string command, string? perfilTerminal = null) =>
        ExecutarComInterface($"sudo {ShellPosix.Resolver()} -l -i -c {ShellPosix.Citar(command)}", perfilTerminal);

    public List<string> ObterPerfisTerminal() => Terminal.ObterPerfis();

    public void AbrirDiretorio(string caminho)
    {
        var executavel = ResolverGerenciadorArquivos(Environment.GetEnvironmentVariable("PATH") ?? "") ?? "xdg-open";
        AbrirComAmbienteGrafico(executavel, caminho);
    }

    public void AbrirArquivo(string caminho)
    {
        var executavel = ResolverEditorTexto(Environment.GetEnvironmentVariable("PATH") ?? "") ?? "xdg-open";
        AbrirComAmbienteGrafico(executavel, caminho);
    }

    private static void AbrirComAmbienteGrafico(string executavel, string caminho)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executavel,
            Arguments = $"\"{caminho}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        AmbienteGrafico.Aplicar(psi);

        Process.Start(psi);
    }

    internal static string? ResolverEditorTexto(string caminhoPath)
    {
        var diretorios = caminhoPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var editor in EditoresTexto)
            foreach (var diretorio in diretorios)
                if (File.Exists(Path.Combine(diretorio, editor)))
                    return editor;

        return null;
    }

    internal static string? ResolverGerenciadorArquivos(string caminhoPath)
    {
        var diretorios = caminhoPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var gerenciador in GerenciadoresArquivos)
            foreach (var diretorio in diretorios)
                if (File.Exists(Path.Combine(diretorio, gerenciador)))
                    return gerenciador;

        return null;
    }

    public void RecarregarTerminal() => _terminal = null;

    private static async Task RegistrarResultado(Process processo, string comando)
    {
        try
        {
            await processo.WaitForExitAsync();
            ShellExecute.LogComando(comando, processo.ExitCode == 0 ? "OK" : $"ERRO ({processo.ExitCode})");
        }
        catch (Exception ex)
        {
            ShellExecute.LogComando(comando, $"ERRO: {ex.Message}");
        }
        finally
        {
            processo.Dispose();
        }
    }

    private ITerminalEmulator ResolverTerminal()
    {
        var config = configuracaoService.ObterConfiguracaoAsync().GetAwaiter().GetResult();
        return TerminalEmulatorFactory.Criar(config.TerminalLinux);
    }

    internal static string? ExtrairDiretorioDoComando(string command)
    {
        var match = Regex.Match(command, @"cd\s+""([^""]+)""");
        return match.Success ? match.Groups[1].Value : null;
    }
}

namespace ProjectManagerWeb.src.Utils;

public class ShellExecute
{
    private static readonly string LogFilePath =
        Path.Combine(PathHelper.BancoPath, "comandos-executados.txt");

    private static readonly object _logLock = new();

    private static IShellProvider _provider = null!;

    public static void Configure(IShellProvider provider) =>
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public static void ExecutarComando(string command, string? perfilTerminal = null, string? githubToken = null)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        var comandoFinal = InjetarToken(command, githubToken);

        if (comandoFinal.Contains("Exit;", StringComparison.OrdinalIgnoreCase))
            ExecutarComandoSemInterface(comandoFinal);
        else
            ExecutarComandoComInterface(comandoFinal, perfilTerminal);
    }

    public static void ExecutarComandoComInterface(string command, string? perfilTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        LogComando(command, "SOLICITADO", perfilTerminal);

        try
        {
            _provider.ExecutarComInterface(command, perfilTerminal);
        }
        catch (Exception ex)
        {
            LogComando(command, $"ERRO: {ex.Message}", perfilTerminal);
            throw new Exception($"Erro ao executar o comando: {ex.Message}", ex);
        }
    }

    public static void ExecutarComandoComoAdministrador(string command, string? perfilTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        LogComando(command, "ADMIN SOLICITADO", perfilTerminal);

        try
        {
            _provider.ExecutarComoAdministrador(command, perfilTerminal);
        }
        catch (Exception ex)
        {
            LogComando(command, $"ADMIN ERRO: {ex.Message}", perfilTerminal);
            throw new Exception($"Erro ao executar o comando como administrador: {ex.Message}", ex);
        }
    }

    public static void ExecutarComandoSemInterface(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        LogComando(command, "SOLICITADO");

        try
        {
            _provider.ExecutarSemInterface(command);
        }
        catch (Exception ex)
        {
            LogComando(command, $"ERRO: {ex.Message}");
            throw new Exception($"Erro ao executar o comando: {ex.Message}", ex);
        }
    }

    private static string InjetarToken(string command, string? githubToken)
    {
        if (string.IsNullOrWhiteSpace(githubToken)) return command;

        var prefix = OperatingSystem.IsWindows()
            ? $"$env:GH_TOKEN=\"{githubToken}\"; "
            : $"export GH_TOKEN=\"{githubToken}\"; ";

        return prefix + command;
    }

    public static void LogComando(string command, string status = "SOLICITADO", string? perfilTerminal = null)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var perfil = !string.IsNullOrWhiteSpace(perfilTerminal) ? $" [Perfil: {perfilTerminal}]" : "";
            var logEntry = $"[{timestamp}] {status}{perfil} - {command}{Environment.NewLine}";

            lock (_logLock)
            {
                File.AppendAllText(LogFilePath, logEntry);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao registrar comando no log: {ex.Message}");
        }
    }
}

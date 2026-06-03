namespace ProjectManagerWeb.src.Utils;

public class ShellExecute
{
    private static readonly string LogFilePath =
        Path.Combine(PathHelper.BancoPath, "comandos-executados.txt");

    private static IShellProvider _provider = null!;

    public static void Configure(IShellProvider provider) =>
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public static void ExecutarComando(string command, string? perfilTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        if (command.Contains("Exit;", StringComparison.OrdinalIgnoreCase))
            ExecutarComandoSemInterface(command);
        else
            ExecutarComandoComInterface(command, perfilTerminal);
    }

    public static void ExecutarComandoComInterface(string command, string? perfilTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        try
        {
            _ = Task.Run(() => LogComandoAsync(command, "SOLICITADO", perfilTerminal));
            _provider.ExecutarComInterface(command, perfilTerminal);
        }
        catch (Exception ex)
        {
            _ = Task.Run(() => LogComandoAsync(command, $"ERRO: {ex.Message}", perfilTerminal));
            throw new Exception($"Erro ao executar o comando: {ex.Message}", ex);
        }
    }

    public static void ExecutarComandoComoAdministrador(string command, string? perfilTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        try
        {
            _ = Task.Run(() => LogComandoAsync(command, "ADMIN SOLICITADO", perfilTerminal));
            _provider.ExecutarComoAdministrador(command, perfilTerminal);
        }
        catch (Exception ex)
        {
            _ = Task.Run(() => LogComandoAsync(command, $"ADMIN ERRO: {ex.Message}", perfilTerminal));
            throw new Exception($"Erro ao executar o comando como administrador: {ex.Message}", ex);
        }
    }

    public static void ExecutarComandoSemInterface(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        try
        {
            _ = Task.Run(() => LogComandoAsync(command, "SOLICITADO"));
            _provider.ExecutarSemInterface(command);
        }
        catch (Exception ex)
        {
            _ = Task.Run(() => LogComandoAsync(command, $"ERRO: {ex.Message}"));
            throw new Exception($"Erro ao executar o comando: {ex.Message}", ex);
        }
    }

    public static async Task LogComandoAsync(string command, string status = "SOLICITADO", string? perfilTerminal = null)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var perfil = !string.IsNullOrWhiteSpace(perfilTerminal) ? $" [Perfil: {perfilTerminal}]" : "";
            var logEntry = $"[{timestamp}] {status}{perfil} - {command}{Environment.NewLine}";

            await File.AppendAllTextAsync(LogFilePath, logEntry);
        }
        catch
        {
        }
    }
}

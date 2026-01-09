using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils;

public class ShellExecute
{
    private static readonly string LogFilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PMW", "Banco", "comandos-executados.txt");

    /// <summary>
    /// Abre o PowerShell através do CMD para executar um comando Git, mantendo o console aberto.
    /// </summary>
    /// <param name="command">O comando a ser executado.</param>
    public static void ExecutarComando(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        // Verifica se o comando contém "Exit;" para decidir qual método usar
        if (command.Contains("Exit;", StringComparison.OrdinalIgnoreCase))
        {
            ExecutarComandoSemInterface(command);
        }
        else
        {
            ExecutarComandoComInterface(command);
        }
    }

    /// <summary>
    /// Executa um comando PowerShell com interface visível, mantendo o console aberto.
    /// </summary>
    /// <param name="command">O comando a ser executado.</param>
    public static void ExecutarComandoComInterface(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        // Log assíncrono sem bloquear
        _ = Task.Run(() => LogComandoAsync(command));

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = $"-NoExit -Command \"{command}\"",
                UseShellExecute = true
            };

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Log de erro
            _ = Task.Run(() => LogComandoAsync(command, $"ERRO: {ex.Message}"));
            throw new Exception($"Erro ao executar o comando: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executa um comando PowerShell com privilégios de administrador, mantendo o console aberto.
    /// </summary>
    /// <param name="command">O comando a ser executado.</param>
    public static void ExecutarComandoComoAdministrador(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        // Log assíncrono sem bloquear
        _ = Task.Run(() => LogComandoAsync(command, "ADMIN SOLICITADO"));

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = $"-NoExit -Command \"{command}\"",
                UseShellExecute = true,
                Verb = "runas" // Solicita elevação de privilégios
            };

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Log de erro
            _ = Task.Run(() => LogComandoAsync(command, $"ADMIN ERRO: {ex.Message}"));
            throw new Exception($"Erro ao executar o comando como administrador: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executa um comando PowerShell sem interface visível (em background).
    /// </summary>
    /// <param name="command">O comando a ser executado.</param>
    public static void ExecutarComandoSemInterface(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        // Log assíncrono sem bloquear
        _ = Task.Run(() => LogComandoAsync(command));

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = $"-WindowStyle Hidden -Command \"{command}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Log de erro
            _ = Task.Run(() => LogComandoAsync(command, $"ERRO: {ex.Message}"));
            throw new Exception($"Erro ao executar o comando: {ex.Message}", ex);
        }
    }

    private static async Task LogComandoAsync(string command, string status = "SOLICITADO")
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"[{timestamp}] {status} - {command}{Environment.NewLine}";
            
            await File.AppendAllTextAsync(LogFilePath, logEntry);
        }
        catch
        {
            // Ignora erros de log para não quebrar a funcionalidade principal
        }
    }
}
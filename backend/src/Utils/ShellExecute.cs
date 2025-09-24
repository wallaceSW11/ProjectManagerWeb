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
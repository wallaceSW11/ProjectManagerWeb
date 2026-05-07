using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils;

public class ShellExecute
{
    private static readonly string LogFilePath =
        Path.Combine(PathHelper.BancoPath, "comandos-executados.txt");

    /// <summary>
    /// Abre o PowerShell através do CMD para executar um comando Git, mantendo o console aberto.
    /// </summary>
    /// <param name="command">O comando a ser executado.</param>
    /// <param name="perfilTerminal">Perfil do Windows Terminal (opcional).</param>
    public static void ExecutarComando(string command, string? perfilTerminal = null)
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
            ExecutarComandoComInterface(command, perfilTerminal);
        }
    }

    /// <summary>
    /// Executa um comando PowerShell com interface visível, mantendo o console aberto.
    /// Se um perfil do Windows Terminal for informado, abre via wt.exe como nova aba.
    /// </summary>
    /// <param name="command">O comando a ser executado.</param>
    /// <param name="perfilTerminal">Perfil do Windows Terminal (opcional).</param>
    public static void ExecutarComandoComInterface(string command, string? perfilTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        _ = Task.Run(() => LogComandoAsync(command, "SOLICITADO", perfilTerminal));

        try
        {
            ProcessStartInfo psi;

            if (!string.IsNullOrWhiteSpace(perfilTerminal))
            {
                var encodedCommand = Convert.ToBase64String(
                    System.Text.Encoding.Unicode.GetBytes(command));

                psi = new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = $"-w 0 new-tab -p \"{perfilTerminal}\" pwsh -NoExit -EncodedCommand {encodedCommand}",
                    UseShellExecute = true
                };
            }
            else
            {
                psi = new ProcessStartInfo
                {
                    FileName = "pwsh.exe",
                    Arguments = $"-NoExit -Command \"{command}\"",
                    UseShellExecute = true
                };
            }

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _ = Task.Run(() => LogComandoAsync(command, $"ERRO: {ex.Message}", perfilTerminal));
            throw new Exception($"Erro ao executar o comando: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executa um comando PowerShell com privilégios de administrador, mantendo o console aberto.
    /// Se um perfil do Windows Terminal for informado, abre via wt.exe (o perfil deve ter elevate=true).
    /// </summary>
    /// <param name="command">O comando a ser executado.</param>
    /// <param name="perfilTerminal">Perfil do Windows Terminal com privilégio admin (opcional).</param>
    public static void ExecutarComandoComoAdministrador(string command, string? perfilTerminal = null)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        // Log assíncrono sem bloquear
        _ = Task.Run(() => LogComandoAsync(command, "ADMIN SOLICITADO", perfilTerminal));

        try
        {
            ProcessStartInfo psi;

            if (!string.IsNullOrWhiteSpace(perfilTerminal))
            {
                var encodedCommand = Convert.ToBase64String(
                    System.Text.Encoding.Unicode.GetBytes(command));

                psi = new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = $"-w 0 new-tab -p \"{perfilTerminal}\" pwsh -NoExit -EncodedCommand {encodedCommand}",
                    UseShellExecute = true
                };
            }
            else
            {
                psi = new ProcessStartInfo
                {
                    FileName = "pwsh.exe",
                    Arguments = $"-NoExit -Command \"{command}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };
            }

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Log de erro
            _ = Task.Run(() => LogComandoAsync(command, $"ADMIN ERRO: {ex.Message}", perfilTerminal));
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
            var encodedCommand = Convert.ToBase64String(
                System.Text.Encoding.Unicode.GetBytes(command));

            var psi = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = $"-WindowStyle Hidden -EncodedCommand {encodedCommand}",
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

    private static async Task LogComandoAsync(string command, string status = "SOLICITADO", string? perfilTerminal = null)
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
            // Ignora erros de log para não quebrar a funcionalidade principal
        }
    }
}
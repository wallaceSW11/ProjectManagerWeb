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
    /// Fire-and-forget — não aguarda o processo terminar.
    /// </summary>
    /// <param name="command">O comando a ser executado.</param>
    public static void ExecutarComandoComInterface(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

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
            _ = Task.Run(() => LogComandoAsync(command, $"ERRO: {ex.Message}"));
            throw new Exception($"Erro ao executar o comando: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executa um comando PowerShell com janela visível (usuário acompanha) e AGUARDA terminar.
    /// Usa arquivo sentinela: o comando escreve um arquivo ao concluir, o backend faz polling.
    /// Ideal para git clone — janela aberta + sequenciamento garantido.
    /// </summary>
    public static async Task ExecutarComandoAguardarAsync(string command, int timeoutMs = 300_000)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        _ = Task.Run(() => LogComandoAsync(command));

        // Arquivo de script e sentinela temporários — evita problemas de escape no -Command
        var id = Guid.NewGuid().ToString("N");
        var script   = Path.Combine(Path.GetTempPath(), $"jarvas_{id}.ps1");
        var sentinela = Path.Combine(Path.GetTempPath(), $"jarvas_{id}.done");

        // Escreve o script: executa o comando e cria o sentinela ao terminar
        await File.WriteAllTextAsync(script,
            $"{command}\nNew-Item -ItemType File -Path '{sentinela}' -Force | Out-Null\n");

        var psi = new ProcessStartInfo
        {
            FileName = "pwsh.exe",
            Arguments = $"-NoExit -ExecutionPolicy Bypass -File \"{script}\"",
            UseShellExecute = true
        };

        using var process = Process.Start(psi)!;

        // Polling no arquivo sentinela (verifica a cada 500ms)
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!File.Exists(sentinela))
        {
            if (DateTime.UtcNow > deadline)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw new TimeoutException($"Comando excedeu {timeoutMs / 1000}s");
            }
            await Task.Delay(500);
        }

        // Limpa arquivos temporários
        try { File.Delete(sentinela); File.Delete(script); } catch { }
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
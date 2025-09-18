using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils;

public class ShellExecute
{
    /// <summary>
    /// Abre o PowerShell através do CMD para executar um comando Git, mantendo o console aberto.
    /// </summary>
    /// <param name="gitCommand">O comando Git a ser executado (ex: "status", "version").</param>
    /// <param name="workingDirectory">O diretório de trabalho onde o comando será executado. O padrão é o diretório atual.</param>
    public static void ExecutarComando(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(command));

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = $"-NoExit -Command \"{command}\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao executar o comando: {ex.Message}", ex);
        }
    }
}
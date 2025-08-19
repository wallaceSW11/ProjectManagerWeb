using System;
using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils;

public class ShellExecute
{
    /// <summary>
    /// Abre o PowerShell através do CMD para executar um comando Git, mantendo o console aberto.
    /// </summary>
    /// <param name="gitCommand">O comando Git a ser executado (ex: "status", "version").</param>
    /// <param name="workingDirectory">O diretório de trabalho onde o comando será executado. O padrão é o diretório atual.</param>
    public static void ExecutarComando(string gitCommand)
    {
        if (string.IsNullOrWhiteSpace(gitCommand))
        {
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(gitCommand));
        }

        try
        {
            // Constrói o comando completo a ser executado pelo PowerShell
            string powerShellCommand = $"powershell.exe -NoExit -Command \"C: {gitCommand}\"";

            ProcessStartInfo processStartInfo = new("cmd.exe")
            {
                // O argumento /K mantém o CMD aberto após a execução do comando
                Arguments = $"/K {powerShellCommand}",
                // Define o diretório de trabalho, se especificado
                WorkingDirectory = "",
                // UseShellExecute deve ser true para abrir uma nova janela de console
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ocorreu um erro ao tentar executar o comando: {ex.Message}");
        }
    }
}
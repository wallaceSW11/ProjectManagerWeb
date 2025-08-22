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
    public static void ExecutarComando(string gitCommand, bool ocultar = false)
    {
        if (string.IsNullOrWhiteSpace(gitCommand))
        {
            throw new ArgumentException("O comando não pode ser nulo ou vazio.", nameof(gitCommand));
        }

        try
        {
            string arguments = $"/C start pwsh.exe -NoExit -Command \"C: ; {gitCommand}; {(ocultar ? "; exit" : "")}\"";

            ProcessStartInfo psi = new("cmd.exe")
            {
                Arguments = arguments,
                UseShellExecute = true,
                WindowStyle = ocultar ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Minimized
            };

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ocorreu um erro ao tentar executar o comando: {ex.Message}");
        }
    }
}
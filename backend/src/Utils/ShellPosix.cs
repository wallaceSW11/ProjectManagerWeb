using System.Text;

namespace ProjectManagerWeb.src.Utils;

internal static class ShellPosix
{
    private static readonly string[] Suportados = ["bash", "zsh", "ksh", "dash", "sh"];
    private static readonly string[] Fallbacks = ["/bin/bash", "/usr/bin/bash", "/bin/sh"];

    internal static string Resolver()
    {
        var shell = Environment.GetEnvironmentVariable("SHELL");

        if (EhSuportado(shell)) return shell!;

        var fallback = Fallbacks.FirstOrDefault(File.Exists) ?? "bash";

        ShellExecute.LogComando($"shell '{shell}' não suportado; usando '{fallback}'", "AVISO");

        return fallback;
    }

    internal static bool EhSuportado(string? caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho) || !Path.IsPathRooted(caminho)) return false;

        return Suportados.Contains(Path.GetFileName(caminho)) && File.Exists(caminho);
    }

    internal static string Citar(string valor) =>
        $"'{valor.Replace("'", "'\\''")}'";

    internal static List<string> SepararArgumentos(string? linha)
    {
        var argumentos = new List<string>();
        if (string.IsNullOrWhiteSpace(linha)) return argumentos;

        var atual = new StringBuilder();
        var aspas = '\0';
        var iniciado = false;

        foreach (var caractere in linha)
        {
            if (aspas != '\0')
            {
                if (caractere == aspas)
                {
                    aspas = '\0';
                    continue;
                }

                atual.Append(caractere);
                continue;
            }

            if (caractere is '"' or '\'')
            {
                aspas = caractere;
                iniciado = true;
                continue;
            }

            if (char.IsWhiteSpace(caractere))
            {
                if (!iniciado) continue;

                argumentos.Add(atual.ToString());
                atual.Clear();
                iniciado = false;
                continue;
            }

            atual.Append(caractere);
            iniciado = true;
        }

        if (iniciado) argumentos.Add(atual.ToString());

        return argumentos;
    }
}

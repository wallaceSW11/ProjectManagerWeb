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
}

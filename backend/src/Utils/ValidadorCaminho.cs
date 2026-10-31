namespace ProjectManagerWeb.src.Utils;

public static class ValidadorCaminho
{
    private static StringComparison Comparacao =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static bool EstaContidoEm(string caminho, string raiz)
    {
        if (string.IsNullOrWhiteSpace(caminho) || string.IsNullOrWhiteSpace(raiz))
            return false;

        var caminhoCompleto = Path.GetFullPath(caminho);
        var raizCompleta = Path.GetFullPath(raiz);

        if (string.Equals(caminhoCompleto, raizCompleta, Comparacao))
            return true;

        return caminhoCompleto.StartsWith(raizCompleta + Path.DirectorySeparatorChar, Comparacao);
    }

    public static string Normalizar(string caminho) =>
        Path.GetFullPath(caminho).TrimEnd(Path.DirectorySeparatorChar);
}

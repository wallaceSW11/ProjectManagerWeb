namespace ProjectManagerWeb.src.Utils;

public static class PathHelper
{
    private static string _bancoPath = null!;
    private static readonly object _lock = new();

    public static string BancoPath
    {
        get
        {
            if (_bancoPath is null)
                lock (_lock)
                    if (_bancoPath is null)
                        Configure("Production");
            return _bancoPath!;
        }
    }

    public static void Configure(string environment)
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PMW"
        );
        var bancoDir = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase)
            ? "Banco_Dev"
            : "Banco";

        Interlocked.Exchange(ref _bancoPath, Path.Combine(basePath, bancoDir));
    }
}

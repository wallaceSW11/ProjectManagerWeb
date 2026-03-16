namespace ProjectManagerWeb.src.Utils;

public static class PathHelper
{
    public static string BancoPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PMW",
            "Banco"
        );
}

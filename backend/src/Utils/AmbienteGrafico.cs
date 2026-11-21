using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils;

internal static class AmbienteGrafico
{
    private static readonly string[] Variaveis = ["DISPLAY", "XAUTHORITY", "WAYLAND_DISPLAY", "XDG_RUNTIME_DIR", "DBUS_SESSION_BUS_ADDRESS"];

    internal static void Aplicar(ProcessStartInfo psi)
    {
        var doSystemd = Variaveis.Any(variavel => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variavel)))
            ? AmbienteSystemd()
            : [];

        foreach (var variavel in Variaveis)
        {
            var valor = Environment.GetEnvironmentVariable(variavel) ?? doSystemd.GetValueOrDefault(variavel, string.Empty);
            if (string.IsNullOrWhiteSpace(valor)) continue;

            psi.Environment[variavel] = valor;
        }
    }

    private static Dictionary<string, string> AmbienteSystemd()
    {
        var saida = ExecutarProcesso("systemctl", "--user show-environment");
        if (string.IsNullOrWhiteSpace(saida)) return [];

        return saida
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(linha => linha.Split('=', 2))
            .Where(partes => partes.Length == 2)
            .ToDictionary(partes => partes[0].Trim(), partes => partes[1].Trim());
    }

    private static string ExecutarProcesso(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null) return string.Empty;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return output;
        }
        catch
        {
            return string.Empty;
        }
    }
}

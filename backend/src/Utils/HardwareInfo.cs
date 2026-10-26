using System.Management;
using System.Runtime.Versioning;

namespace ProjectManagerWeb.src.Utils;

public static class HardwareInfo
{
    private const string CaminhoCpuInfo = "/proc/cpuinfo";

    private static int? _nucleosFisicos;

    public static int NucleosFisicos => _nucleosFisicos ??= DetectarNucleosFisicos();

    private static int DetectarNucleosFisicos()
    {
        var detectados = OperatingSystem.IsWindows() ? DetectarWindows() : DetectarLinux();
        return detectados > 0 ? detectados : Environment.ProcessorCount;
    }

    [SupportedOSPlatform("windows")]
    private static int DetectarWindows()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT NumberOfCores FROM Win32_Processor");
            var total = 0;

            foreach (var item in searcher.Get())
            {
                using var objeto = (ManagementObject)item;
                total += Convert.ToInt32(objeto["NumberOfCores"] ?? 0);
            }

            return total;
        }
        catch
        {
            return 0;
        }
    }

    private static int DetectarLinux()
    {
        try
        {
            if (!File.Exists(CaminhoCpuInfo)) return 0;

            return ContarNucleos(File.ReadLines(CaminhoCpuInfo));
        }
        catch
        {
            return 0;
        }
    }

    internal static int ContarNucleos(IEnumerable<string> linhas)
    {
        var nucleos = new HashSet<string>();
        string? physicalId = null;

        foreach (var linha in linhas)
        {
            var partes = linha.Split(':', 2);
            if (partes.Length != 2) continue;

            var chave = partes[0].Trim();
            var valor = partes[1].Trim();

            if (chave == "physical id")
                physicalId = valor;
            else if (chave == "core id")
                nucleos.Add($"{physicalId ?? "0"}:{valor}");
        }

        return nucleos.Count;
    }
}

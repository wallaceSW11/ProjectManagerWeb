using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

[SupportedOSPlatform("windows")]
internal class WindowsCpuRamColetor : ICpuRamColetor
{
    private const string ConsultaTemperatura = "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature";

    private PerformanceCounter? _contadorPerformanceProcessor;
    private string? _nomeCpu;
    private int? _clockMaxMhz;

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    private long _idleAnterior;
    private long _kernelAnterior;
    private long _userAnterior;
    private bool _possuiAmostraAnterior;

    public string ObterSistemaOperacional() => RuntimeInformation.OSDescription;

    public string? ObterCpuNome()
    {
        if (_nomeCpu is null)
            _nomeCpu = ConsultarTextoUnico("SELECT Name FROM Win32_Processor", "Name");
        return _nomeCpu;
    }

    public double? ObterCpuFrequenciaMhz()
    {
        if (_clockMaxMhz is null)
            _clockMaxMhz = ConsultarInteiroUnico("SELECT MaxClockSpeed FROM Win32_Processor", "MaxClockSpeed");

        var clockMaxMhz = _clockMaxMhz;
        if (clockMaxMhz is null || clockMaxMhz <= 0)
            return null;

        try
        {
            _contadorPerformanceProcessor ??= new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
            var desempenho = _contadorPerformanceProcessor.NextValue();
            if (desempenho > 0)
                return clockMaxMhz * desempenho / 100.0;
        }
        catch
        {
        }

        return ConsultarInteiroUnico("SELECT CurrentClockSpeed FROM Win32_Processor", "CurrentClockSpeed");
    }

    public double? ObterCpuTemperaturaCelsius()
    {
        var decimosKelvin = ConsultarInteiroUnico(ConsultaTemperatura, "CurrentTemperature");
        if (decimosKelvin is null or <= 0)
            return null;

        var celsius = decimosKelvin.Value / 10.0 - 273.15;
        return celsius is > -50 and < 150 ? celsius : null;
    }

    public double? ObterCpuPercentual()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
            return null;

        if (!_possuiAmostraAnterior)
        {
            _idleAnterior = idle;
            _kernelAnterior = kernel;
            _userAnterior = user;
            _possuiAmostraAnterior = true;
            return null;
        }

        var deltaIdle = idle - _idleAnterior;
        var deltaTotal = kernel + user - _kernelAnterior - _userAnterior;
        _idleAnterior = idle;
        _kernelAnterior = kernel;
        _userAnterior = user;

        if (deltaTotal == 0)
            return null;

        return (1.0 - (double)deltaIdle / deltaTotal) * 100.0;
    }

    public (long total, long disponivel) ObterMemoria()
    {
        var status = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status))
            return (0, 0);

        return ((long)status.ullTotalPhys, (long)status.ullAvailPhys);
    }

    private static string? ConsultarTextoUnico(string consulta, string propriedade) =>
        ConsultarValorUnico(consulta, propriedade)?.ToString();

    private static int? ConsultarInteiroUnico(string consulta, string propriedade)
    {
        var valor = ConsultarValorUnico(consulta, propriedade);
        if (valor is null)
            return null;
        return Convert.ToInt32(valor);
    }

    private static object? ConsultarValorUnico(string consulta, string propriedade)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(consulta);
            using var resultados = searcher.Get();
            foreach (var item in resultados.Cast<ManagementObject>())
            {
                using (item)
                {
                    var valor = item[propriedade];
                    return valor is null or 0 ? null : valor;
                }
            }
        }
        catch
        {
        }

        return null;
    }
}

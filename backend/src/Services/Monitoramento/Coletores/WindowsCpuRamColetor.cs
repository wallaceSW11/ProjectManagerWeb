using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using LibreHardwareMonitor.Hardware;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

[SupportedOSPlatform("windows")]
internal class WindowsCpuRamColetor : ICpuRamColetor
{
    private const string EscopoWmiRaiz = @"root\WMI";
    private const string ConsultaTemperaturaMsAcpi = "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature";
    private const string ConsultaTemperaturaPerf = "SELECT Temperature FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation";

    private PerformanceCounter? _contadorPerformanceProcessor;
    private string? _nomeCpu;
    private int? _clockMaxMhz;
    private Computer? _computador;
    private bool _tentouAbrirComputador;
    private double? _temperaturaWmi;
    private bool _temperaturaWmiDefinitiva;

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

    public string ObterSistemaOperacional()
    {
        var nome = ConsultarTextoUnico("SELECT Caption FROM Win32_OperatingSystem", "Caption");
        if (string.IsNullOrWhiteSpace(nome))
            return RuntimeInformation.OSDescription;

        const string prefixoMicrosoft = "Microsoft ";
        return nome.StartsWith(prefixoMicrosoft, StringComparison.OrdinalIgnoreCase)
            ? nome[prefixoMicrosoft.Length..]
            : nome;
    }

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
        var celsius = LerTemperaturaCpu();
        if (celsius is not null)
            return celsius;

        if (_temperaturaWmiDefinitiva)
            return _temperaturaWmi;

        _temperaturaWmi = LerTemperaturaWmi();
        if (_temperaturaWmi is null)
            _temperaturaWmiDefinitiva = true;

        return _temperaturaWmi;
    }

    private double? LerTemperaturaWmi()
    {
        var decimosKelvin = ConsultarInteiroUnico(ConsultaTemperaturaMsAcpi, "CurrentTemperature", EscopoWmiRaiz)
            ?? ConsultarInteiroUnico(ConsultaTemperaturaPerf, "Temperature");
        if (decimosKelvin is null or <= 0)
            return null;

        var celsius = decimosKelvin.Value / 10.0 - 273.15;
        return celsius is > -50 and < 150 ? celsius : null;
    }

    private double? LerTemperaturaCpu()
    {
        var computador = AtualizarSensores();
        if (computador is null)
            return null;

        double? maior = null;
        foreach (var hardware in computador.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu)
                continue;

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType != SensorType.Temperature || sensor.Value is null)
                    continue;

                var celsius = (double)sensor.Value.Value;
                if (!double.IsFinite(celsius) || celsius is <= 0 or > 150)
                    continue;

                maior = maior is null ? celsius : Math.Max(maior.Value, celsius);
            }
        }

        return maior;
    }

    public double? ObterDiscoTemperaturaCelsius()
    {
        var computador = AtualizarSensores();
        if (computador is null)
            return null;

        double? maior = null;
        foreach (var hardware in computador.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Storage)
                continue;

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType != SensorType.Temperature || sensor.Value is null)
                    continue;

                if (sensor.Name.Contains("Warning", StringComparison.OrdinalIgnoreCase) ||
                    sensor.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase))
                    continue;

                var celsius = (double)sensor.Value.Value;
                if (!double.IsFinite(celsius) || celsius is < 0 or > 120)
                    continue;

                maior = maior is null ? celsius : Math.Max(maior.Value, celsius);
            }
        }

        return maior;
    }

    public (long total, long usado) ObterSwap() => (0, 0);

    private Computer? AtualizarSensores()
    {
        if (!_tentouAbrirComputador)
        {
            _computador = AbrirComputador();
            _tentouAbrirComputador = true;
        }

        if (_computador is null)
            return null;

        try
        {
            _computador.Accept(new UpdateVisitor());
            return _computador;
        }
        catch
        {
            return null;
        }
    }

    private static Computer? AbrirComputador()
    {
        try
        {
            var computador = new Computer { IsCpuEnabled = true, IsStorageEnabled = true };
            computador.Open();
            return computador;
        }
        catch
        {
            return null;
        }
    }

    public double? ObterRamVelocidadeMhz()
    {
        var velocidade = ConsultarInteiroUnico("SELECT ConfiguredClockSpeed FROM Win32_PhysicalMemory", "ConfiguredClockSpeed");
        if (velocidade is not null)
            return velocidade;
        return ConsultarInteiroUnico("SELECT Speed FROM Win32_PhysicalMemory", "Speed");
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

    private static int? ConsultarInteiroUnico(string consulta, string propriedade, string? escopo = null)
    {
        var valor = ConsultarValorUnico(consulta, propriedade, escopo);
        if (valor is null)
            return null;
        return Convert.ToInt32(valor);
    }

    private static object? ConsultarValorUnico(string consulta, string propriedade, string? escopo = null)
    {
        try
        {
            using var searcher = escopo is null
                ? new ManagementObjectSearcher(consulta)
                : new ManagementObjectSearcher(escopo, consulta);
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

internal class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer) => computer.Traverse(this);

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var subHardware in hardware.SubHardware)
            subHardware.Accept(this);
    }

    public void VisitSensor(ISensor sensor) { }

    public void VisitParameter(IParameter parameter) { }
}

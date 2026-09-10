using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

[SupportedOSPlatform("windows")]
internal class WindowsCpuRamColetor(ILogger<WindowsCpuRamColetor> logger) : ICpuRamColetor
{
    private const string EscopoWmiRaiz = @"root\WMI";
    private const string ConsultaTemperaturaMsAcpi = "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature";
    private const string ConsultaTemperaturaPerf = "SELECT Temperature FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation";
    private const uint IoctlConsultaPropriedadeStorage = 0x002D1400;
    private const int PropriedadeTemperaturaDisco = 22;
    private const int ConsultaPadraoPropriedade = 0;
    private const int MaximoDiscosFisicos = 16;
    private const int TamanhoCabecalhoTemperatura = 16;
    private const int TamanhoInfoTemperatura = 12;
    private const uint AcessoLeituraAtributos = 0x80;
    private const uint CompartilhamentoLeitura = 0x1;
    private const uint CompartilhamentoEscrita = 0x2;
    private const uint AbrirExistente = 3;
    private static readonly TimeSpan IntervaloFallbackWmi = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IntervaloAtualizacaoDisco = TimeSpan.FromSeconds(10);

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        ref StoragePropertyQuery lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [StructLayout(LayoutKind.Sequential)]
    private struct StoragePropertyQuery
    {
        public int PropertyId;
        public int QueryType;
        public byte AdditionalParameters;
    }

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

    private PerformanceCounter? _contadorPerformanceProcessor;
    private string? _sistemaOperacional;
    private string? _nomeCpu;
    private int? _clockMaxMhz;
    private double? _temperaturaWmi;
    private bool _temperaturaWmiDefinitiva;
    private DateTime _proximaConsultaTemperaturaWmi = DateTime.MinValue;
    private double? _frequenciaFallbackWmi;
    private DateTime _proximaConsultaFrequenciaWmi = DateTime.MinValue;
    private double? _ramVelocidadeMhz;
    private bool _tentouLerRamVelocidade;
    private double? _discoTemperatura;
    private bool _discoTemperaturaDefinitiva;
    private DateTime _proximaConsultaDisco = DateTime.MinValue;
    private long _idleAnterior;
    private long _kernelAnterior;
    private long _userAnterior;
    private bool _possuiAmostraAnterior;

    public string ObterSistemaOperacional() => _sistemaOperacional ??= LerSistemaOperacional();

    private static string LerSistemaOperacional()
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
        _clockMaxMhz ??= ConsultarInteiroUnico("SELECT MaxClockSpeed FROM Win32_Processor", "MaxClockSpeed");

        var clockMaxMhz = _clockMaxMhz;
        if (clockMaxMhz is null || clockMaxMhz <= 0)
            return null;

        var desempenho = LerDesempenhoProcessador();
        if (desempenho > 0)
            return clockMaxMhz * desempenho / 100.0;

        return ObterFrequenciaFallbackWmi();
    }

    private float? LerDesempenhoProcessador()
    {
        try
        {
            _contadorPerformanceProcessor ??= new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
            return _contadorPerformanceProcessor.NextValue();
        }
        catch
        {
            return null;
        }
    }

    private double? ObterFrequenciaFallbackWmi()
    {
        if (DateTime.UtcNow < _proximaConsultaFrequenciaWmi)
            return _frequenciaFallbackWmi;

        _frequenciaFallbackWmi = ConsultarInteiroUnico("SELECT CurrentClockSpeed FROM Win32_Processor", "CurrentClockSpeed");
        _proximaConsultaFrequenciaWmi = DateTime.UtcNow + IntervaloFallbackWmi;
        return _frequenciaFallbackWmi;
    }

    public double? ObterCpuTemperaturaCelsius()
    {
        if (_temperaturaWmiDefinitiva || DateTime.UtcNow < _proximaConsultaTemperaturaWmi)
            return _temperaturaWmi;

        _temperaturaWmi = LerTemperaturaWmi();
        _proximaConsultaTemperaturaWmi = DateTime.UtcNow + IntervaloFallbackWmi;
        if (_temperaturaWmi is null)
        {
            _temperaturaWmiDefinitiva = true;
            logger.LogInformation("Temperatura de CPU não disponível via ACPI nesta máquina; exibindo --");
        }

        return _temperaturaWmi;
    }

    private static double? LerTemperaturaWmi()
    {
        var decimosKelvin = ConsultarInteiroUnico(ConsultaTemperaturaMsAcpi, "CurrentTemperature", EscopoWmiRaiz)
            ?? ConsultarInteiroUnico(ConsultaTemperaturaPerf, "Temperature");
        if (decimosKelvin is null or <= 0)
            return null;

        var celsius = decimosKelvin.Value / 10.0 - 273.15;
        return celsius is > -50 and < 150 ? celsius : null;
    }

    public double? ObterDiscoTemperaturaCelsius()
    {
        if (_discoTemperaturaDefinitiva || DateTime.UtcNow < _proximaConsultaDisco)
            return _discoTemperatura;

        _discoTemperatura = LerTemperaturaDisco();
        _proximaConsultaDisco = DateTime.UtcNow + IntervaloAtualizacaoDisco;
        if (_discoTemperatura is null)
        {
            _discoTemperaturaDefinitiva = true;
            logger.LogInformation("Temperatura de disco não disponível; exibindo --");
        }

        return _discoTemperatura;
    }

    private static double? LerTemperaturaDisco()
    {
        double? maior = null;

        for (var numero = 0; numero < MaximoDiscosFisicos; numero++)
        {
            using var disco = CreateFileW(
                $@"\\.\PhysicalDrive{numero}",
                AcessoLeituraAtributos,
                CompartilhamentoLeitura | CompartilhamentoEscrita,
                IntPtr.Zero,
                AbrirExistente,
                0,
                IntPtr.Zero);

            if (disco.IsInvalid)
                continue;

            var temperatura = LerTemperaturaDiscoHandle(disco);
            if (temperatura is not null)
                maior = maior is null ? temperatura : Math.Max(maior.Value, temperatura.Value);
        }

        return maior;
    }

    private static double? LerTemperaturaDiscoHandle(SafeFileHandle disco)
    {
        var buffer = Marshal.AllocHGlobal(TamanhoCabecalhoTemperatura + TamanhoInfoTemperatura * 8);
        try
        {
            var consulta = new StoragePropertyQuery
            {
                PropertyId = PropriedadeTemperaturaDisco,
                QueryType = ConsultaPadraoPropriedade
            };

            var tamanhoBuffer = (uint)(TamanhoCabecalhoTemperatura + TamanhoInfoTemperatura * 8);
            if (!DeviceIoControl(disco, IoctlConsultaPropriedadeStorage, ref consulta, (uint)Marshal.SizeOf<StoragePropertyQuery>(), buffer, tamanhoBuffer, out var retornado, IntPtr.Zero))
                return null;

            var tamanhoRetornado = (int)retornado;
            if (tamanhoRetornado < TamanhoCabecalhoTemperatura)
                return null;

            double? maior = null;
            var quantidade = (ushort)Marshal.ReadInt16(buffer, 12);
            for (var indice = 0; indice < quantidade; indice++)
            {
                var offset = TamanhoCabecalhoTemperatura + indice * TamanhoInfoTemperatura;
                if (offset + TamanhoInfoTemperatura > tamanhoRetornado)
                    break;

                var temperatura = Marshal.ReadInt16(buffer, offset + 2);
                if (temperatura is <= 0 or > 120)
                    continue;

                maior = maior is null ? temperatura : Math.Max(maior.Value, temperatura);
            }

            return maior;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public (long total, long usado) ObterSwap() => (0, 0);

    public double? ObterRamVelocidadeMhz()
    {
        if (!_tentouLerRamVelocidade)
        {
            _ramVelocidadeMhz = LerRamVelocidadeMhz();
            _tentouLerRamVelocidade = true;
        }

        return _ramVelocidadeMhz;
    }

    private static double? LerRamVelocidadeMhz()
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

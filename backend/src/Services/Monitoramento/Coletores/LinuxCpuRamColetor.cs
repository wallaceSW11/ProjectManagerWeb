using System.Globalization;
using System.Runtime.InteropServices;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

internal class LinuxCpuRamColetor : ICpuRamColetor
{
    private const string CaminhoStat = "/proc/stat";
    private const string CaminhoMeminfo = "/proc/meminfo";
    private const string CaminhoOsRelease = "/etc/os-release";
    private const string CaminhoCpuinfo = "/proc/cpuinfo";
    private const string CaminhoScalingCurFreq = "/sys/devices/system/cpu/cpu0/cpufreq/scaling_cur_freq";
    private const string CaminhoZonasTermicas = "/sys/class/thermal";
    private const string CaminhoHwmon = "/sys/class/hwmon";

    private readonly string _caminhoStat;
    private readonly string _caminhoMeminfo;
    private readonly string _caminhoOsRelease;
    private readonly string _caminhoCpuinfo;
    private readonly string _caminhoScalingCurFreq;
    private readonly string _caminhoZonasTermicas;
    private readonly string _caminhoHwmon;

    private long _userAnterior;
    private long _niceAnterior;
    private long _systemAnterior;
    private long _idleAnterior;
    private long _iowaitAnterior;
    private long _irqAnterior;
    private long _softirqAnterior;
    private long _stealAnterior;
    private bool _possuiAmostraAnterior;
    private readonly ValidadorCoolerDinamico _validadorCooler = new();

    public LinuxCpuRamColetor() : this(CaminhoStat, CaminhoMeminfo, CaminhoOsRelease)
    {
    }

    internal LinuxCpuRamColetor(
        string caminhoStat,
        string caminhoMeminfo,
        string caminhoOsRelease,
        string caminhoCpuinfo = CaminhoCpuinfo,
        string caminhoScalingCurFreq = CaminhoScalingCurFreq,
        string caminhoZonasTermicas = CaminhoZonasTermicas,
        string caminhoHwmon = CaminhoHwmon)
    {
        _caminhoStat = caminhoStat;
        _caminhoMeminfo = caminhoMeminfo;
        _caminhoOsRelease = caminhoOsRelease;
        _caminhoCpuinfo = caminhoCpuinfo;
        _caminhoScalingCurFreq = caminhoScalingCurFreq;
        _caminhoZonasTermicas = caminhoZonasTermicas;
        _caminhoHwmon = caminhoHwmon;
    }

    public string ObterSistemaOperacional()
    {
        try
        {
            foreach (var linha in File.ReadLines(_caminhoOsRelease))
            {
                if (linha.StartsWith("PRETTY_NAME=", StringComparison.Ordinal))
                    return linha["PRETTY_NAME=".Length..].Trim('"');
            }
        }
        catch
        {
        }

        return RuntimeInformation.OSDescription;
    }

    public string? ObterCpuNome()
    {
        try
        {
            foreach (var linha in File.ReadLines(_caminhoCpuinfo))
            {
                if (linha.StartsWith("model name", StringComparison.Ordinal))
                    return ExtrairValor(linha);
                if (linha.StartsWith("Hardware", StringComparison.Ordinal))
                    return ExtrairValor(linha);
            }
        }
        catch
        {
        }

        return null;
    }

    public double? ObterCpuFrequenciaMhz()
    {
        try
        {
            double soma = 0;
            int contagem = 0;
            foreach (var linha in File.ReadLines(_caminhoCpuinfo))
            {
                if (!linha.StartsWith("cpu MHz", StringComparison.Ordinal))
                    continue;

                var valor = ExtrairValor(linha);
                if (double.TryParse(valor, CultureInfo.InvariantCulture, out var mhz))
                {
                    soma += mhz;
                    contagem++;
                }
            }

            if (contagem > 0)
                return soma / contagem;
        }
        catch
        {
        }

        return LerFrequenciaScalingCurFreq();
    }

    public double? ObterCpuTemperaturaCelsius() =>
        LerTemperaturaZonasTermicas() ?? LerTemperaturaHwmon();

    private double? LerTemperaturaZonasTermicas()
    {
        if (!Directory.Exists(_caminhoZonasTermicas))
            return null;

        double? temperaturaFallback = null;

        try
        {
            foreach (var zona in Directory.GetDirectories(_caminhoZonasTermicas, "thermal_zone*"))
            {
                var celsius = LerCelsius(Path.Combine(zona, "temp"));
                if (celsius is null)
                    continue;

                var tipo = LerTexto(Path.Combine(zona, "type"));
                if (tipo.Contains("cpu", StringComparison.OrdinalIgnoreCase)
                    || tipo.Contains("pkg", StringComparison.OrdinalIgnoreCase))
                    return celsius;

                temperaturaFallback ??= celsius;
            }
        }
        catch
        {
            return null;
        }

        return temperaturaFallback;
    }

    private double? LerTemperaturaHwmon()
    {
        if (!Directory.Exists(_caminhoHwmon))
            return null;

        double? temperaturaFallback = null;

        try
        {
            foreach (var hwmon in Directory.GetDirectories(_caminhoHwmon, "hwmon*"))
            {
                var celsius = LerCelsius(Path.Combine(hwmon, "temp1_input"));
                if (celsius is null)
                    continue;

                var nome = LerTexto(Path.Combine(hwmon, "name"));
                if (nome.Contains("k10temp", StringComparison.OrdinalIgnoreCase)
                    || nome.Contains("coretemp", StringComparison.OrdinalIgnoreCase)
                    || nome.Contains("cpu", StringComparison.OrdinalIgnoreCase))
                    return celsius;

                temperaturaFallback ??= celsius;
            }
        }
        catch
        {
            return null;
        }

        return temperaturaFallback;
    }

    private static double? LerCelsius(string caminho)
    {
        var milesimosDeGrau = LerValorInteiro(caminho);
        if (milesimosDeGrau is null or <= 0)
            return null;
        return milesimosDeGrau.Value / 1000.0;
    }

    public double? ObterRamVelocidadeMhz() => null;

    public double? ObterCoolerRpm()
    {
        if (!Directory.Exists(_caminhoHwmon))
            return null;

        long? maiorRpm = null;

        try
        {
            foreach (var hwmon in Directory.GetDirectories(_caminhoHwmon, "hwmon*"))
            {
                foreach (var fan in Directory.GetFiles(hwmon, "fan*_input"))
                {
                    var rpm = LerValorInteiro(fan);
                    if (rpm is null or <= 1)
                        continue;

                    maiorRpm = maiorRpm is null ? rpm : Math.Max(maiorRpm.Value, rpm.Value);
                }
            }
        }
        catch
        {
            return null;
        }

        return _validadorCooler.Avaliar(maiorRpm);
    }

    public (long total, long usado) ObterSwap()
    {
        var total = LerValorMeminfo("SwapTotal:") * 1024;
        var livre = LerValorMeminfo("SwapFree:") * 1024;
        return (total, total - livre);
    }

    public double? ObterDiscoTemperaturaCelsius()
    {
        if (!Directory.Exists(_caminhoHwmon))
            return null;

        try
        {
            foreach (var hwmon in Directory.GetDirectories(_caminhoHwmon, "hwmon*"))
            {
                var nome = LerTexto(Path.Combine(hwmon, "name"));
                if (!nome.Contains("nvme", StringComparison.OrdinalIgnoreCase))
                    continue;

                var celsius = LerCelsius(Path.Combine(hwmon, "temp1_input"));
                if (celsius is not null)
                    return celsius;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    public double? ObterCpuPercentual()
    {
        var amostra = LerAmostraCpu();
        if (amostra is null)
            return null;

        if (!_possuiAmostraAnterior)
        {
            SalvarAmostra(amostra.Value);
            return null;
        }

        var totalIdleAtual = amostra.Value.Idle + amostra.Value.Iowait;
        var totalAtivoAtual = amostra.Value.User + amostra.Value.Nice + amostra.Value.System
            + amostra.Value.Irq + amostra.Value.Softirq + amostra.Value.Steal;
        var totalAtual = totalIdleAtual + totalAtivoAtual;

        var totalIdleAnterior = _idleAnterior + _iowaitAnterior;
        var totalAtivoAnterior = _userAnterior + _niceAnterior + _systemAnterior
            + _irqAnterior + _softirqAnterior + _stealAnterior;
        var totalAnterior = totalIdleAnterior + totalAtivoAnterior;

        SalvarAmostra(amostra.Value);

        var deltaTotal = totalAtual - totalAnterior;
        var deltaIdle = totalIdleAtual - totalIdleAnterior;
        if (deltaTotal == 0)
            return null;

        return (1.0 - (double)deltaIdle / deltaTotal) * 100.0;
    }

    public (long total, long disponivel) ObterMemoria()
    {
        var total = LerValorMeminfo("MemTotal:") * 1024;
        var disponivel = LerValorMeminfo("MemAvailable:") * 1024;
        return (total, disponivel);
    }

    private AmostraCpu? LerAmostraCpu()
    {
        try
        {
            var linha = File.ReadLines(_caminhoStat)
                .FirstOrDefault(l => l.StartsWith("cpu ", StringComparison.Ordinal));
            if (linha is null)
                return null;

            var partes = linha.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length < 9)
                return null;

            return new AmostraCpu(
                long.Parse(partes[1]),
                long.Parse(partes[2]),
                long.Parse(partes[3]),
                long.Parse(partes[4]),
                long.Parse(partes[5]),
                long.Parse(partes[6]),
                long.Parse(partes[7]),
                long.Parse(partes[8])
            );
        }
        catch
        {
            return null;
        }
    }

    private long LerValorMeminfo(string chave)
    {
        try
        {
            var linha = File.ReadLines(_caminhoMeminfo)
                .FirstOrDefault(l => l.StartsWith(chave, StringComparison.Ordinal));
            if (linha is null)
                return 0;

            var partes = linha.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length >= 2 && long.TryParse(partes[1], out var valor) ? valor : 0;
        }
        catch
        {
            return 0;
        }
    }

    private void SalvarAmostra(AmostraCpu amostra)
    {
        _userAnterior = amostra.User;
        _niceAnterior = amostra.Nice;
        _systemAnterior = amostra.System;
        _idleAnterior = amostra.Idle;
        _iowaitAnterior = amostra.Iowait;
        _irqAnterior = amostra.Irq;
        _softirqAnterior = amostra.Softirq;
        _stealAnterior = amostra.Steal;
        _possuiAmostraAnterior = true;
    }

    private double? LerFrequenciaScalingCurFreq()
    {
        try
        {
            if (!File.Exists(_caminhoScalingCurFreq))
                return null;

            if (long.TryParse(File.ReadAllText(_caminhoScalingCurFreq).Trim(), out var khz) && khz > 0)
                return khz / 1000.0;
        }
        catch
        {
        }

        return null;
    }

    private static string ExtrairValor(string linha)
    {
        var partes = linha.Split(':', 2, StringSplitOptions.TrimEntries);
        return partes.Length == 2 ? partes[1] : "";
    }

    private static long? LerValorInteiro(string caminho)
    {
        try
        {
            if (!File.Exists(caminho))
                return null;

            return long.TryParse(File.ReadAllText(caminho).Trim(), out var valor) ? valor : null;
        }
        catch
        {
            return null;
        }
    }

    private static string LerTexto(string caminho)
    {
        try
        {
            return File.Exists(caminho) ? File.ReadAllText(caminho).Trim() : "";
        }
        catch
        {
            return "";
        }
    }

    private readonly record struct AmostraCpu(
        long User,
        long Nice,
        long System,
        long Idle,
        long Iowait,
        long Irq,
        long Softirq,
        long Steal
    );
}

using System.Runtime.InteropServices;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

internal class LinuxCpuRamColetor : ICpuRamColetor
{
    private const string CaminhoStat = "/proc/stat";
    private const string CaminhoMeminfo = "/proc/meminfo";
    private const string CaminhoOsRelease = "/etc/os-release";

    private readonly string _caminhoStat;
    private readonly string _caminhoMeminfo;
    private readonly string _caminhoOsRelease;

    private long _userAnterior;
    private long _niceAnterior;
    private long _systemAnterior;
    private long _idleAnterior;
    private long _iowaitAnterior;
    private long _irqAnterior;
    private long _softirqAnterior;
    private long _stealAnterior;
    private bool _possuiAmostraAnterior;

    public LinuxCpuRamColetor() : this(CaminhoStat, CaminhoMeminfo, CaminhoOsRelease)
    {
    }

    internal LinuxCpuRamColetor(string caminhoStat, string caminhoMeminfo, string caminhoOsRelease)
    {
        _caminhoStat = caminhoStat;
        _caminhoMeminfo = caminhoMeminfo;
        _caminhoOsRelease = caminhoOsRelease;
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

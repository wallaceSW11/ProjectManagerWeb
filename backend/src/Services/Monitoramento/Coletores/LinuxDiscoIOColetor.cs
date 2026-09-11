using System.Diagnostics;
using System.Globalization;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

internal class LinuxDiscoIOColetor : IDiscoIOColetor
{
    private const string CaminhoDiskstatsPadrao = "/proc/diskstats";
    private const string CaminhoBlockPadrao = "/sys/block";
    private const long BytesPorSetor = 512;
    private static readonly TimeSpan IntervaloAtualizacaoDispositivos = TimeSpan.FromSeconds(30);

    private readonly string _caminhoDiskstats;
    private readonly string _caminhoBlock;
    private readonly Stopwatch _cronometro = Stopwatch.StartNew();
    private long _bytesLidosAnterior;
    private long _bytesEscritosAnterior;
    private long _tempoLeituraAnterior;
    private long _tempoOcupadoAnterior;
    private long _leiturasAnterior;
    private bool _possuiAmostraAnterior;
    private HashSet<string> _dispositivos = [];
    private DateTime _proximaAtualizacaoDispositivos = DateTime.MinValue;

    public LinuxDiscoIOColetor() : this(CaminhoDiskstatsPadrao, CaminhoBlockPadrao)
    {
    }

    internal LinuxDiscoIOColetor(string caminhoDiskstats, string caminhoBlock)
    {
        _caminhoDiskstats = caminhoDiskstats;
        _caminhoBlock = caminhoBlock;
    }

    public (long? leituraBytesPorSegundo, long? escritaBytesPorSegundo, double? atividadePercentual, double? latenciaLeituraMs) ObterMetricas()
    {
        var (bytesLidos, bytesEscritos, tempoLeitura, tempoOcupado, leituras) = LerTotais();
        var decorridoSegundos = _cronometro.Elapsed.TotalSeconds;
        _cronometro.Restart();

        if (!_possuiAmostraAnterior)
        {
            SalvarAmostra(bytesLidos, bytesEscritos, tempoLeitura, tempoOcupado, leituras);
            return (null, null, null, null);
        }

        var deltaBytesLidos = Math.Max(0, bytesLidos - _bytesLidosAnterior);
        var deltaBytesEscritos = Math.Max(0, bytesEscritos - _bytesEscritosAnterior);
        var deltaTempoLeitura = Math.Max(0, tempoLeitura - _tempoLeituraAnterior);
        var deltaTempoOcupado = Math.Max(0, tempoOcupado - _tempoOcupadoAnterior);
        var deltaLeituras = Math.Max(0, leituras - _leiturasAnterior);
        SalvarAmostra(bytesLidos, bytesEscritos, tempoLeitura, tempoOcupado, leituras);

        if (decorridoSegundos <= 0)
            return (null, null, null, null);

        var leitura = (long)(deltaBytesLidos / decorridoSegundos);
        var escrita = (long)(deltaBytesEscritos / decorridoSegundos);
        var atividade = Math.Clamp(deltaTempoOcupado / (decorridoSegundos * 1000.0) * 100.0, 0.0, 100.0);
        var latencia = deltaLeituras > 0
            ? deltaTempoLeitura / (double)deltaLeituras
            : (double?)null;

        return (leitura, escrita, atividade, latencia);
    }

    private (long bytesLidos, long bytesEscritos, long tempoLeitura, long tempoOcupado, long leituras) LerTotais()
    {
        long bytesLidos = 0;
        long bytesEscritos = 0;
        long tempoLeitura = 0;
        long tempoOcupado = 0;
        long leituras = 0;

        try
        {
            var dispositivos = ObterDispositivos();
            foreach (var linha in File.ReadLines(_caminhoDiskstats))
            {
                var campos = linha.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (campos.Length < 14 || !dispositivos.Contains(campos[2]))
                    continue;

                bytesLidos += long.Parse(campos[5], CultureInfo.InvariantCulture) * BytesPorSetor;
                tempoLeitura += long.Parse(campos[6], CultureInfo.InvariantCulture);
                bytesEscritos += long.Parse(campos[9], CultureInfo.InvariantCulture) * BytesPorSetor;
                tempoOcupado += long.Parse(campos[12], CultureInfo.InvariantCulture);
                leituras += long.Parse(campos[3], CultureInfo.InvariantCulture);
            }
        }
        catch
        {
            return (0, 0, 0, 0, 0);
        }

        return (bytesLidos, bytesEscritos, tempoLeitura, tempoOcupado, leituras);
    }

    private HashSet<string> ObterDispositivos()
    {
        if (DateTime.UtcNow < _proximaAtualizacaoDispositivos)
            return _dispositivos;

        try
        {
            _dispositivos = [.. Directory.GetDirectories(_caminhoBlock).Select(Path.GetFileName).OfType<string>()];
        }
        catch
        {
            _dispositivos = [];
        }

        _proximaAtualizacaoDispositivos = DateTime.UtcNow + IntervaloAtualizacaoDispositivos;
        return _dispositivos;
    }

    private void SalvarAmostra(long bytesLidos, long bytesEscritos, long tempoLeitura, long tempoOcupado, long leituras)
    {
        _bytesLidosAnterior = bytesLidos;
        _bytesEscritosAnterior = bytesEscritos;
        _tempoLeituraAnterior = tempoLeitura;
        _tempoOcupadoAnterior = tempoOcupado;
        _leiturasAnterior = leituras;
        _possuiAmostraAnterior = true;
    }
}

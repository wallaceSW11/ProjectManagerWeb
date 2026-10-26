using System.Diagnostics;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.src.Services.Monitoramento.Uso;

public class UsoRecursosService(
    ConfiguracaoService configuracaoService,
    UsoJsonService usoJsonService,
    IProcessosColetor processosColetor,
    ILogger<UsoRecursosService> logger) : IHostedService
{
    private static readonly TimeSpan IntervaloVigia = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IntervaloFlush = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ToleranciaIntervalo = TimeSpan.FromMilliseconds(250);
    private const int QuantidadeTopProcessos = 10;
    private const int TamanhoMaximoBuffer = 300;
    private const int RetencaoPadraoDias = 30;

    private readonly object _bufferLock = new();
    private readonly List<UsoAmostraDTO> _buffer = [];

    private CancellationTokenSource? _cts;
    private Task? _vigia;

    private DateTimeOffset _ultimoRegistro = DateTimeOffset.MinValue;
    private DateTimeOffset _ultimoFlush = DateTimeOffset.MinValue;
    private DateTimeOffset _instanteAmostraProcessos = DateTimeOffset.MinValue;
    private DateTimeOffset _instanteCpuPmw = DateTimeOffset.MinValue;
    private TimeSpan _cpuPmwAnterior;
    private Dictionary<int, AmostraProcessoDTO>? _amostraProcessosAnterior;
    private MaquinaDTO? _maquina;

    public Task StartAsync(CancellationToken ct)
    {
        _ultimoFlush = DateTimeOffset.Now;
        _cts = new CancellationTokenSource();
        _vigia = VigiarAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _cts?.Cancel();
        if (_vigia is not null)
            await _vigia;

        await FlushAsync(CancellationToken.None);

        _cts?.Dispose();
        _cts = null;
        _vigia = null;
    }

    public void Registrar(MonitoramentoSnapshotDTO snapshot, int coletaMs)
    {
        var uso = configuracaoService.ConfiguracaoEmCache?.UsoRecursos;
        if (uso is null || !uso.Ativo) return;

        if (snapshot.CpuPercentual is null || snapshot.CpuPercentual.Value < uso.LimiarCpuPercentual) return;

        var agora = DateTimeOffset.Now;
        var intervalo = TimeSpan.FromSeconds(Math.Max(uso.IntervaloRegistroSegundos, 1)) - ToleranciaIntervalo;
        if (agora - _ultimoRegistro < intervalo) return;

        _ultimoRegistro = agora;
        _maquina ??= MaquinaDTO.DoSnapshot(snapshot);

        var amostra = MontarAmostra(snapshot, coletaMs, agora);

        lock (_bufferLock)
            _buffer.Add(amostra);
    }

    public async Task FlushAsync(CancellationToken ct = default)
    {
        var lote = RetirarBuffer();
        if (lote.Count == 0) return;

        try
        {
            await usoJsonService.GravarLoteAsync(lote, _maquina, RetencaoDias(), ct);
            _ultimoFlush = DateTimeOffset.Now;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao gravar amostras de uso de recursos");
        }
    }

    private async Task VigiarAsync(CancellationToken ct)
    {
        await AtualizarConfiguracaoAsync(ct);

        using var timer = new PeriodicTimer(IntervaloVigia);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    await AtualizarConfiguracaoAsync(ct);
                    AtualizarPendentes();

                    if (DeveGravar())
                        await FlushAsync(ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Falha no ciclo de registro de uso de recursos");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private async Task AtualizarConfiguracaoAsync(CancellationToken ct)
    {
        try
        {
            await configuracaoService.ObterConfiguracaoEmCacheAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao recarregar a configuração de uso de recursos");
        }
    }

    private void AtualizarPendentes()
    {
        if (ContarPendentes() == 0) return;

        var agora = DateTimeOffset.Now;
        var topProcessos = AmostrarProcessos(agora);
        var cpuPmw = CalcularCpuPmw(agora);

        lock (_bufferLock)
        {
            if (_buffer.Count == 0) return;

            if (topProcessos is not null)
                _buffer[^1] = _buffer[^1] with { Processos = topProcessos };

            for (var i = 0; i < _buffer.Count; i++)
                _buffer[i] = _buffer[i] with { PmwCpu = cpuPmw };
        }
    }

    private List<UsoProcessoDTO>? AmostrarProcessos(DateTimeOffset agora)
    {
        var atual = processosColetor.ObterAmostraCpu();
        var anterior = _amostraProcessosAnterior;
        var decorridoSegundos = (agora - _instanteAmostraProcessos).TotalSeconds;

        _amostraProcessosAnterior = atual;
        _instanteAmostraProcessos = agora;

        if (anterior is null) return null;

        return [.. TopProcessosCpu.Calcular(anterior, atual, decorridoSegundos, QuantidadeTopProcessos)
            .Where(processo => processo.Percentual > 0)
            .Select(processo => new UsoProcessoDTO(processo.Nome, Math.Round(processo.Percentual, 1)))];
    }

    private double CalcularCpuPmw(DateTimeOffset agora)
    {
        using var processo = Process.GetCurrentProcess();
        var cpuAtual = processo.TotalProcessorTime;

        if (_instanteCpuPmw == DateTimeOffset.MinValue)
        {
            _cpuPmwAnterior = cpuAtual;
            _instanteCpuPmw = agora;
            return 0;
        }

        var decorridoSegundos = (agora - _instanteCpuPmw).TotalSeconds;
        var deltaSegundos = (cpuAtual - _cpuPmwAnterior).TotalSeconds;

        _cpuPmwAnterior = cpuAtual;
        _instanteCpuPmw = agora;

        if (decorridoSegundos <= 0) return 0;

        return Math.Round(deltaSegundos / decorridoSegundos / Environment.ProcessorCount * 100.0, 2);
    }

    private bool DeveGravar()
    {
        lock (_bufferLock)
        {
            if (_buffer.Count == 0) return false;
            return _buffer.Count >= TamanhoMaximoBuffer
                || DateTimeOffset.Now - _ultimoFlush >= IntervaloFlush - ToleranciaIntervalo;
        }
    }

    private int ContarPendentes()
    {
        lock (_bufferLock)
            return _buffer.Count;
    }

    private List<UsoAmostraDTO> RetirarBuffer()
    {
        lock (_bufferLock)
        {
            if (_buffer.Count == 0) return [];

            var lote = new List<UsoAmostraDTO>(_buffer);
            _buffer.Clear();
            return lote;
        }
    }

    private int RetencaoDias() =>
        configuracaoService.ConfiguracaoEmCache?.UsoRecursos?.RetencaoDias ?? RetencaoPadraoDias;

    private static UsoAmostraDTO MontarAmostra(MonitoramentoSnapshotDTO snapshot, int coletaMs, DateTimeOffset agora) =>
        new(
            agora,
            snapshot.CpuPercentual,
            CalcularRamPercentual(snapshot),
            snapshot.RamUsadaBytes,
            snapshot.DiscoDisponivelBytes,
            snapshot.DiscoAtividadePercentual,
            snapshot.DiscoLatenciaLeituraMs,
            snapshot.CpuFrequenciaMhz,
            snapshot.CpuTemperaturaCelsius,
            snapshot.DiscoTemperaturaCelsius,
            coletaMs,
            0,
            null);

    private static double? CalcularRamPercentual(MonitoramentoSnapshotDTO snapshot)
    {
        if (snapshot.RamTotalBytes is null or 0 || snapshot.RamUsadaBytes is null) return null;
        return Math.Round(snapshot.RamUsadaBytes.Value * 100.0 / snapshot.RamTotalBytes.Value, 1);
    }
}

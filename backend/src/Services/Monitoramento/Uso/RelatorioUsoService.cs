using System.Diagnostics;
using System.Globalization;
using System.Text;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Uso;

public class RelatorioUsoService
{
    private const int LimiteRegistrosTabela = 10000;
    private const int QuantidadeTopProcessos = 10;
    private const string FormatoData = "yyyy-MM-dd";
    private static readonly TimeSpan LacunaEpisodio = TimeSpan.FromMinutes(2);
    private static readonly Encoding Codificacao = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly UsoJsonService _usoJsonService;
    private readonly ConfiguracaoService _configuracaoService;
    private readonly IColetorMetricas _coletor;
    private readonly ILogger<RelatorioUsoService> _logger;
    private readonly Action<string> _abrirArquivo;

    public RelatorioUsoService(
        UsoJsonService usoJsonService,
        ConfiguracaoService configuracaoService,
        IColetorMetricas coletor,
        ILogger<RelatorioUsoService> logger)
        : this(usoJsonService, configuracaoService, coletor, logger, AbrirComShell)
    {
    }

    internal RelatorioUsoService(
        UsoJsonService usoJsonService,
        ConfiguracaoService configuracaoService,
        IColetorMetricas coletor,
        ILogger<RelatorioUsoService> logger,
        Action<string> abrirArquivo)
    {
        _usoJsonService = usoJsonService;
        _configuracaoService = configuracaoService;
        _coletor = coletor;
        _logger = logger;
        _abrirArquivo = abrirArquivo;
    }

    public async Task<RelatorioUsoResponseDTO> GerarAsync(RelatorioUsoRequestDTO request, CancellationToken ct)
    {
        var dia = ConverterData(request.Data) ?? DateOnly.FromDateTime(DateTime.Now);

        var maquinaColetada = await ColetarMaquinaAsync(ct);
        var acumulador = new Acumulador(maquinaColetada?.RamTotalBytes);
        acumulador.Adicionar(await _usoJsonService.LerDiaAsync(dia, ct));

        if (acumulador.Registros == 0)
            throw new InvalidOperationException($"Nenhum registro de uso em {dia.ToString(FormatoData, CultureInfo.InvariantCulture)}.");

        var maquina = maquinaColetada ?? acumulador.Maquina;
        var dados = acumulador.Montar(LimiarCpu(), maquina);
        var html = RelatorioUsoTemplate.Gerar(dados);
        var caminho = Salvar(html, dia);

        AbrirNoNavegador(caminho);

        return new RelatorioUsoResponseDTO(caminho, html.Length, acumulador.Registros);
    }

    private async Task<MaquinaDTO?> ColetarMaquinaAsync(CancellationToken ct)
    {
        try
        {
            return MaquinaDTO.DoSnapshot(await _coletor.ColetarAsync(ct));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível coletar os dados da máquina para o relatório de uso");
            return null;
        }
    }

    private double? LimiarCpu() =>
        _configuracaoService.ConfiguracaoEmCache?.UsoRecursos?.LimiarCpuPercentual;

    private static DateOnly? ConverterData(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;

        return DateOnly.TryParseExact(valor, FormatoData, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data
            : null;
    }

    private string Salvar(string html, DateOnly dia)
    {
        var diretorio = _usoJsonService.DiretorioRelatorios;
        Directory.CreateDirectory(diretorio);

        var nome = $"relatorio-uso-{dia.ToString(FormatoData, CultureInfo.InvariantCulture)}.html";
        var caminho = Path.Combine(diretorio, nome);

        File.WriteAllText(caminho, html, Codificacao);
        return caminho;
    }

    private void AbrirNoNavegador(string caminho)
    {
        try
        {
            _abrirArquivo(caminho);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível abrir o relatório de uso automaticamente");
        }
    }

    private static void AbrirComShell(string caminho) =>
        Process.Start(new ProcessStartInfo(caminho) { UseShellExecute = true });


    private sealed class Acumulador
    {
        private readonly List<RegistroRelatorio> _registros = [];
        private readonly Dictionary<string, double> _processos = [];
        private readonly long? _ramTotalBytes;
        private DateTimeOffset? _anterior;
        private int _omitidos;
        private double _minutos;

        public Acumulador(long? ramTotalBytes)
        {
            _ramTotalBytes = ramTotalBytes;
        }

        public int Registros { get; private set; }
        public MaquinaDTO? Maquina { get; private set; }

        public void Adicionar(List<UsoAmostraDTO> amostras)
        {
            if (amostras.Count == 0) return;

            Maquina ??= amostras.FirstOrDefault(amostra => amostra.Maquina is not null)?.Maquina;

            foreach (var amostra in amostras)
                AdicionarAmostra(amostra);
        }

        public DadosRelatorio Montar(double? limiarCpu, MaquinaDTO? maquina) => new()
        {
            GeradoEm = DateTimeOffset.Now,
            Maquina = ConverterMaquina(maquina),
            RegistrosOmitidos = _omitidos,
            Registros = _registros,
            TopProcessos = MontarTopProcessos(),
            Resumo = new ResumoRelatorio
            {
                Registros = Registros,
                MinutosRegistrados = Arredondar(_minutos, 1),
                LimiarCpu = limiarCpu
            }
        };

        private void AdicionarAmostra(UsoAmostraDTO amostra)
        {
            Registros++;

            TimeSpan? delta = _anterior is null ? null : amostra.Ts - _anterior.Value;
            if (delta is not null && delta <= LacunaEpisodio)
                _minutos += delta.Value.TotalMinutes;

            _anterior = amostra.Ts;

            AcumularProcessos(amostra.Processos);

            if (_registros.Count < LimiteRegistrosTabela)
                _registros.Add(ConverterRegistro(amostra, _ramTotalBytes ?? Maquina?.RamTotalBytes));
            else
                _omitidos++;
        }

        private void AcumularProcessos(List<UsoProcessoDTO>? processos)
        {
            if (processos is null) return;

            foreach (var processo in processos)
                _processos[processo.Nome] = _processos.GetValueOrDefault(processo.Nome) + processo.Cpu;
        }

        private List<string> MontarTopProcessos() =>
            [.. _processos
                .OrderByDescending(par => par.Value)
                .ThenBy(par => par.Key, StringComparer.OrdinalIgnoreCase)
                .Take(QuantidadeTopProcessos)
                .Select(par => par.Key)];

        private static RegistroRelatorio ConverterRegistro(UsoAmostraDTO amostra, long? ramTotalBytes) => new()
        {
            Ts = amostra.Ts,
            Cpu = Arredondar(amostra.Cpu),
            Ram = amostra.RamPercentual,
            RamUsada = amostra.RamUsadaBytes ?? CalcularRamUsada(amostra.RamLivreBytes, ramTotalBytes),
            DiscoLivre = amostra.DiscoLivreBytes,
            Atividade = Arredondar(amostra.DiscoAtividadePercentual),
            Latencia = Arredondar(amostra.DiscoLatenciaLeituraMs),
            Freq = Arredondar(amostra.CpuFrequenciaMhz),
            TempCpu = Arredondar(amostra.CpuTemperaturaCelsius),
            TempDisco = Arredondar(amostra.DiscoTemperaturaCelsius),
            Coleta = amostra.ColetaMs,
            Pmw = amostra.PmwCpu,
            MetricaCpu = amostra.MetricaCpu,
            Proc = amostra.Processos?
                .Select(processo => new ProcessoRelatorio { Nome = processo.Nome, Cpu = processo.Cpu })
                .ToList()
        };

        private static long? CalcularRamUsada(long? ramLivreBytes, long? ramTotalBytes) =>
            ramLivreBytes is null || ramTotalBytes is null ? null : ramTotalBytes - ramLivreBytes;

        private static MaquinaRelatorio? ConverterMaquina(MaquinaDTO? maquina) => maquina is null
            ? null
            : new MaquinaRelatorio
            {
                SistemaOperacional = maquina.SistemaOperacional,
                CpuNome = maquina.CpuNome,
                Nucleos = maquina.Nucleos,
                Threads = maquina.Threads,
                RamTotalBytes = maquina.RamTotalBytes,
                DiscoTotalBytes = maquina.DiscoTotalBytes
            };

        private static double? Arredondar(double? valor, int casas = 1) =>
            valor is null ? null : Math.Round(valor.Value, casas);

        private static double Arredondar(double valor, int casas = 1) => Math.Round(valor, casas);
    }
}

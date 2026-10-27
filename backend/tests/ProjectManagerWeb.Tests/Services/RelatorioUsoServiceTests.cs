using Microsoft.Extensions.Logging;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;
using ProjectManagerWeb.src.Services.Monitoramento;
using ProjectManagerWeb.src.Services.Monitoramento.Uso;

namespace ProjectManagerWeb.Tests.Services;

public class RelatorioUsoServiceTests : IDisposable
{
    private readonly IColetorMetricas _coletor = Substitute.For<IColetorMetricas>();
    private readonly List<string> _caminhosAbertos = [];
    private readonly string _tempPath;
    private readonly string _diretorioUso;
    private readonly ConfiguracaoService _configuracaoService;
    private readonly UsoJsonService _usoJsonService;

    public RelatorioUsoServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        _diretorioUso = Path.Combine(_tempPath, "uso");
        Directory.CreateDirectory(_tempPath);
        _configuracaoService = new ConfiguracaoService(Path.Combine(_tempPath, "Configuracao.json"));
        _usoJsonService = new UsoJsonService(Substitute.For<ILogger<UsoJsonService>>(), _diretorioUso);
        _coletor.ColetarAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CriarSnapshot()));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private RelatorioUsoService CriarServico() =>
        new(
            _usoJsonService,
            _configuracaoService,
            _coletor,
            Substitute.For<ILogger<RelatorioUsoService>>(),
            caminho => _caminhosAbertos.Add(caminho));

    private static MonitoramentoSnapshotDTO CriarSnapshot(long? ramTotal = 16_000_000_000) =>
        new(
            "linux",
            "Linux",
            50,
            ramTotal,
            4_000_000_000,
            null,
            null,
            null,
            null,
            "CPU coletada",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    private static DateTimeOffset MeioDia(int dia) =>
        new(new DateTime(2026, 3, dia, 12, 0, 0, DateTimeKind.Unspecified));

    private static UsoAmostraDTO CriarAmostra(DateTimeOffset ts) =>
        new(ts, 90, 40, 1_000, 5_000, 10, 2, 3_000, 50, 40, 100, 1.5, null);

    private Task GravarAmostrasAsync(DateTimeOffset ts, int quantidade, MaquinaDTO? maquina = null) =>
        _usoJsonService.GravarLoteAsync(
            [.. Enumerable.Range(0, quantidade).Select(indice => CriarAmostra(ts.AddMinutes(indice)))],
            maquina,
            0,
            CancellationToken.None);

    private string CaminhoRelatorioEsperado() =>
        Path.Combine(_diretorioUso, "relatorios", $"relatorio-uso-{DateTime.Now:yyyy-MM-dd}.html");

    private string CaminhoArquivoDia(DateOnly dia) =>
        Path.Combine(_diretorioUso, $"uso-{dia:yyyy-MM-dd}.jsonl");

    public class GerarAsync : RelatorioUsoServiceTests
    {
        [Fact]
        public async Task Deve_gerar_relatorio_com_totais_quando_ha_amostras()
        {
            await GravarAmostrasAsync(DateTimeOffset.Now, 3);

            var resultado = await CriarServico().GerarAsync(new RelatorioUsoRequestDTO(), CancellationToken.None);

            resultado.Registros.Should().Be(3);
            resultado.Caminho.Should().Be(CaminhoRelatorioEsperado());
            resultado.TamanhoBytes.Should().BeGreaterThan(0);
            File.Exists(resultado.Caminho).Should().BeTrue();
        }

        [Fact]
        public async Task Deve_manter_arquivo_unico_quando_gerado_duas_vezes_no_mesmo_dia()
        {
            await GravarAmostrasAsync(DateTimeOffset.Now, 2);
            var sut = CriarServico();

            await sut.GerarAsync(new RelatorioUsoRequestDTO(), CancellationToken.None);
            await sut.GerarAsync(new RelatorioUsoRequestDTO(), CancellationToken.None);

            Directory.GetFiles(Path.Combine(_diretorioUso, "relatorios")).Should().ContainSingle();
        }

        [Fact]
        public async Task Deve_abrir_relatorio_gerado()
        {
            await GravarAmostrasAsync(DateTimeOffset.Now, 1);

            var resultado = await CriarServico().GerarAsync(new RelatorioUsoRequestDTO(), CancellationToken.None);

            _caminhosAbertos.Should().ContainSingle().Which.Should().Be(resultado.Caminho);
        }

        [Fact]
        public async Task Deve_usar_maquina_coletada_quando_arquivo_do_dia_tem_outra_maquina()
        {
            var maquinaDoArquivo = new MaquinaDTO("Linux", "CPU do arquivo", 2, 4, 8_000_000_000, 100_000_000_000);
            await GravarAmostrasAsync(DateTimeOffset.Now, 1, maquinaDoArquivo);

            var resultado = await CriarServico().GerarAsync(new RelatorioUsoRequestDTO(), CancellationToken.None);
            var html = await File.ReadAllTextAsync(resultado.Caminho);

            html.Should().NotContain("CPU do arquivo");
            html.Should().Contain("CPU coletada");
        }

        [Fact]
        public async Task Deve_calcular_ram_usada_quando_registro_legado_tem_apenas_ram_livre()
        {
            var dia = DateOnly.FromDateTime(DateTime.Now);
            Directory.CreateDirectory(_diretorioUso);
            await File.WriteAllTextAsync(
                CaminhoArquivoDia(dia),
                """{"ts":"2026-03-10T15:00:00+00:00","cpu":90,"ramLivreBytes":6000000000,"coletaMs":100,"pmwCpu":0}""" + "\n");

            var resultado = await CriarServico().GerarAsync(new RelatorioUsoRequestDTO(), CancellationToken.None);
            var html = await File.ReadAllTextAsync(resultado.Caminho);

            html.Should().Contain("\"ramUsada\":10000000000");
        }

        [Fact]
        public async Task Deve_usar_limiar_da_configuracao_no_resumo()
        {
            await _configuracaoService.SalvarConfiguracaoAsync(
                new ConfiguracaoRequestDTO(UsoRecursos: new UsoRecursosDTO(Ativo: true, LimiarCpuPercentual: 65)));
            await GravarAmostrasAsync(DateTimeOffset.Now, 1);

            var resultado = await CriarServico().GerarAsync(new RelatorioUsoRequestDTO(), CancellationToken.None);
            var html = await File.ReadAllTextAsync(resultado.Caminho);

            html.Should().Contain("\"limiarCpu\":65");
        }

        [Fact]
        public async Task Deve_detalhar_metrica_de_cpu_do_registro()
        {
            await GravarAmostrasAsync(DateTimeOffset.Now, 1);

            var resultado = await CriarServico().GerarAsync(new RelatorioUsoRequestDTO(), CancellationToken.None);
            var html = await File.ReadAllTextAsync(resultado.Caminho);

            html.Should().Contain("Métrica da CPU");
        }

        [Fact]
        public async Task Deve_considerar_apenas_segundo_dia_quando_data_inicio_informada()
        {
            await GravarAmostrasAsync(MeioDia(10), 2);
            await GravarAmostrasAsync(MeioDia(11), 3);

            var resultado = await CriarServico().GerarAsync(
                new RelatorioUsoRequestDTO(DataInicio: "2026-03-11"),
                CancellationToken.None);

            resultado.Registros.Should().Be(3);
        }
    }

    public class GerarAsyncSemDados : RelatorioUsoServiceTests
    {
        [Fact]
        public async Task Deve_lancar_excecao_quando_nao_ha_dia_gravado()
        {
            var act = () => CriarServico().GerarAsync(new RelatorioUsoRequestDTO(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task Deve_lancar_excecao_quando_arquivo_do_dia_esta_vazio()
        {
            Directory.CreateDirectory(_diretorioUso);
            await File.WriteAllTextAsync(CaminhoArquivoDia(DateOnly.FromDateTime(DateTime.Now)), string.Empty);

            var act = () => CriarServico().GerarAsync(new RelatorioUsoRequestDTO(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}

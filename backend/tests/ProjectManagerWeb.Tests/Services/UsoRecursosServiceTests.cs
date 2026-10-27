using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;
using ProjectManagerWeb.src.Services.Monitoramento.Coletores;
using ProjectManagerWeb.src.Services.Monitoramento.Uso;

namespace ProjectManagerWeb.Tests.Services;

public class UsoRecursosServiceTests : IDisposable
{
    private readonly IProcessosColetor _processosColetor = Substitute.For<IProcessosColetor>();
    private readonly string _tempPath;
    private readonly string _diretorioUso;
    private readonly ConfiguracaoService _configuracaoService;
    private readonly UsoJsonService _usoJsonService;

    public UsoRecursosServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        _diretorioUso = Path.Combine(_tempPath, "uso");
        Directory.CreateDirectory(_tempPath);
        _configuracaoService = new ConfiguracaoService(Path.Combine(_tempPath, "Configuracao.json"));
        _usoJsonService = new UsoJsonService(Substitute.For<ILogger<UsoJsonService>>(), _diretorioUso);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private async Task<UsoRecursosService> CriarServicoAsync(UsoRecursosDTO uso)
    {
        await _configuracaoService.SalvarConfiguracaoAsync(new ConfiguracaoRequestDTO(UsoRecursos: uso));

        return new UsoRecursosService(
            _configuracaoService,
            _usoJsonService,
            _processosColetor,
            Substitute.For<ILogger<UsoRecursosService>>());
    }

    private static MonitoramentoSnapshotDTO CriarSnapshot(
        double? cpu = 90,
        long? ramTotal = 16_000_000_000,
        long? ramUsada = 4_000_000_000) =>
        new(
            "linux",
            "Linux",
            cpu,
            ramTotal,
            ramUsada,
            null,
            null,
            null,
            null,
            "CPU teste",
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

    private static UsoRecursosDTO UsoAtivo(double limiar = 80, int intervalo = 5) =>
        new(Ativo: true, LimiarCpuPercentual: limiar, IntervaloRegistroSegundos: intervalo);

    private async Task<List<UsoAmostraDTO>> LerHojeAsync() =>
        await _usoJsonService.LerDiaAsync(DateOnly.FromDateTime(DateTime.Now), CancellationToken.None);

    private string CaminhoArquivoHoje() =>
        Path.Combine(_diretorioUso, $"uso-{DateTime.Now:yyyy-MM-dd}.jsonl");

    public class Registrar : UsoRecursosServiceTests
    {
        [Fact]
        public async Task Nao_deve_registrar_quando_uso_desativado()
        {
            var sut = await CriarServicoAsync(new UsoRecursosDTO());

            sut.Registrar(CriarSnapshot(), 100);
            await sut.FlushAsync();

            (await LerHojeAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task Nao_deve_registrar_quando_cpu_abaixo_do_limiar()
        {
            var sut = await CriarServicoAsync(UsoAtivo(limiar: 80));

            sut.Registrar(CriarSnapshot(cpu: 50), 100);
            await sut.FlushAsync();

            (await LerHojeAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task Deve_registrar_quando_cpu_igual_ao_limiar()
        {
            var sut = await CriarServicoAsync(UsoAtivo(limiar: 80));

            sut.Registrar(CriarSnapshot(cpu: 80), 100);
            await sut.FlushAsync();

            (await LerHojeAsync()).Should().ContainSingle();
        }

        [Fact]
        public async Task Deve_registrar_quando_cpu_acima_do_limiar()
        {
            var sut = await CriarServicoAsync(UsoAtivo(limiar: 80));

            sut.Registrar(CriarSnapshot(cpu: 95), 100);
            await sut.FlushAsync();

            (await LerHojeAsync()).Should().ContainSingle();
        }

        [Fact]
        public async Task Nao_deve_registrar_quando_cpu_nao_foi_coletada()
        {
            var sut = await CriarServicoAsync(UsoAtivo());

            sut.Registrar(CriarSnapshot(cpu: null), 100);
            await sut.FlushAsync();

            (await LerHojeAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task Deve_registrar_metrica_de_cpu_da_plataforma()
        {
            var sut = await CriarServicoAsync(UsoAtivo());

            sut.Registrar(CriarSnapshot(cpu: 95), 100);
            await sut.FlushAsync();

            var amostra = (await LerHojeAsync()).Should().ContainSingle().Subject;
            amostra.MetricaCpu.Should().Be(OperatingSystem.IsWindows() ? "utility" : "processor-time");
        }

        [Fact]
        public async Task Nao_deve_registrar_quando_intervalo_minimo_nao_atingido()
        {
            var sut = await CriarServicoAsync(UsoAtivo(intervalo: 5));

            sut.Registrar(CriarSnapshot(cpu: 95), 100);
            sut.Registrar(CriarSnapshot(cpu: 95), 100);
            await sut.FlushAsync();

            (await LerHojeAsync()).Should().ContainSingle();
        }
    }

    public class FlushAsync : UsoRecursosServiceTests
    {
        [Fact]
        public async Task Nao_deve_criar_arquivo_quando_nao_ha_amostras()
        {
            var sut = await CriarServicoAsync(UsoAtivo());

            await sut.FlushAsync();

            Directory.Exists(_diretorioUso).Should().BeFalse();
        }

        [Fact]
        public async Task Deve_calcular_ram_percentual_e_gravar_ram_usada()
        {
            var sut = await CriarServicoAsync(UsoAtivo());
            var snapshot = CriarSnapshot(cpu: 95, ramTotal: 8_000_000_000, ramUsada: 2_000_000_000);

            sut.Registrar(snapshot, 100);
            await sut.FlushAsync();

            var amostras = await LerHojeAsync();
            amostras.Should().ContainSingle();
            amostras[0].RamPercentual.Should().Be(25.0);
            amostras[0].RamUsadaBytes.Should().Be(2_000_000_000);
        }

        [Fact]
        public async Task Deve_gravar_maquina_na_primeira_linha_do_dia()
        {
            var sut = await CriarServicoAsync(UsoAtivo());

            sut.Registrar(CriarSnapshot(cpu: 95), 100);
            await sut.FlushAsync();

            var primeiraLinha = File.ReadAllLines(CaminhoArquivoHoje())[0];
            using var json = JsonDocument.Parse(primeiraLinha);

            json.RootElement.GetProperty("maquina").GetProperty("threads").GetInt32().Should().BeGreaterThan(0);
        }
    }
}

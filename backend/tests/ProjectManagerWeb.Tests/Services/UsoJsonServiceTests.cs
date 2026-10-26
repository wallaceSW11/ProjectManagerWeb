using Microsoft.Extensions.Logging;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services.Monitoramento.Uso;

namespace ProjectManagerWeb.Tests.Services;

public class UsoJsonServiceTests : IDisposable
{
    private const string LinhaSemMaquina = """{"ts":"2026-03-10T15:00:00+00:00","cpu":10,"coletaMs":100,"pmwCpu":0}""";
    private const string LinhaLegada = """{"ts":"2026-03-10T15:00:00+00:00","cpu":10,"ramLivreBytes":8000000000,"coletaMs":100,"pmwCpu":0}""";

    private readonly string _tempPath;
    private readonly string _diretorioUso;
    private readonly UsoJsonService _sut;

    public UsoJsonServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        _diretorioUso = Path.Combine(_tempPath, "uso");
        Directory.CreateDirectory(_tempPath);
        _sut = new UsoJsonService(Substitute.For<ILogger<UsoJsonService>>(), _diretorioUso);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private static DateTimeOffset MeioDia(int dia) =>
        new(new DateTime(2026, 3, dia, 12, 0, 0, DateTimeKind.Unspecified));

    private static UsoAmostraDTO CriarAmostra(DateTimeOffset ts) =>
        new(ts, 50, 40, 1_000, 5_000, 10, 2, 3_000, 50, 40, 100, 1.5, null);

    private static MaquinaDTO CriarMaquina() =>
        new("Linux", "CPU teste", 4, 8, 16_000_000_000, 500_000_000_000);

    private string CaminhoArquivo(DateOnly dia) =>
        Path.Combine(_diretorioUso, $"uso-{dia:yyyy-MM-dd}.jsonl");

    private string[] LerLinhas(DateOnly dia) =>
        File.ReadAllLines(CaminhoArquivo(dia));

    public class GravarLoteAsync : UsoJsonServiceTests
    {
        [Fact]
        public async Task Deve_criar_arquivo_do_dia_com_uma_linha_por_amostra()
        {
            var dia = new DateOnly(2026, 3, 10);
            var ts = MeioDia(10);

            await _sut.GravarLoteAsync(
                [CriarAmostra(ts), CriarAmostra(ts.AddMinutes(1))],
                CriarMaquina(),
                0,
                CancellationToken.None);

            File.Exists(CaminhoArquivo(dia)).Should().BeTrue();
            LerLinhas(dia).Should().HaveCount(2);
        }

        [Fact]
        public async Task Nao_deve_gravar_bom_no_inicio_do_arquivo()
        {
            await _sut.GravarLoteAsync([CriarAmostra(MeioDia(10))], null, 0, CancellationToken.None);

            var bytes = await File.ReadAllBytesAsync(CaminhoArquivo(new DateOnly(2026, 3, 10)));

            bytes[0].Should().Be((byte)'{');
        }

        [Fact]
        public async Task Deve_incluir_maquina_apenas_na_primeira_amostra_do_dia()
        {
            var dia = new DateOnly(2026, 3, 10);
            var ts = MeioDia(10);

            await _sut.GravarLoteAsync(
                [CriarAmostra(ts), CriarAmostra(ts.AddMinutes(1))],
                CriarMaquina(),
                0,
                CancellationToken.None);

            var linhas = LerLinhas(dia);
            linhas[0].Should().Contain("\"maquina\"");
            linhas[1].Should().NotContain("\"maquina\"");
        }

        [Fact]
        public async Task Nao_deve_repetir_maquina_quando_ja_gravada_no_mesmo_dia()
        {
            var dia = new DateOnly(2026, 3, 10);
            var ts = MeioDia(10);

            await _sut.GravarLoteAsync([CriarAmostra(ts)], CriarMaquina(), 0, CancellationToken.None);
            await _sut.GravarLoteAsync([CriarAmostra(ts.AddMinutes(1))], CriarMaquina(), 0, CancellationToken.None);

            var linhas = LerLinhas(dia);
            linhas.Should().HaveCount(2);
            linhas[1].Should().NotContain("\"maquina\"");
        }

        [Fact]
        public async Task Deve_incluir_maquina_quando_arquivo_do_dia_ainda_nao_tem()
        {
            var dia = new DateOnly(2026, 3, 10);
            Directory.CreateDirectory(_diretorioUso);
            await File.WriteAllTextAsync(CaminhoArquivo(dia), LinhaSemMaquina + "\n");

            await _sut.GravarLoteAsync([CriarAmostra(MeioDia(10))], CriarMaquina(), 0, CancellationToken.None);

            var linhas = LerLinhas(dia);
            linhas.Should().HaveCount(2);
            linhas[1].Should().Contain("\"maquina\"");
        }

        [Fact]
        public async Task Deve_criar_um_arquivo_por_dia_quando_lote_tem_dias_diferentes()
        {
            await _sut.GravarLoteAsync(
                [CriarAmostra(MeioDia(10)), CriarAmostra(MeioDia(11))],
                CriarMaquina(),
                0,
                CancellationToken.None);

            File.Exists(CaminhoArquivo(new DateOnly(2026, 3, 10))).Should().BeTrue();
            File.Exists(CaminhoArquivo(new DateOnly(2026, 3, 11))).Should().BeTrue();
        }
    }

    public class ListarDias : UsoJsonServiceTests
    {
        [Fact]
        public async Task Deve_retornar_dias_em_ordem_crescente()
        {
            await _sut.GravarLoteAsync(
                [CriarAmostra(MeioDia(11)), CriarAmostra(MeioDia(9))],
                null,
                0,
                CancellationToken.None);

            var dias = _sut.ListarDias();

            dias.Should().Equal(new DateOnly(2026, 3, 9), new DateOnly(2026, 3, 11));
        }

        [Fact]
        public async Task Deve_ignorar_arquivos_fora_do_padrao_quando_lista_dias()
        {
            var dia = new DateOnly(2026, 3, 10);
            await _sut.GravarLoteAsync([CriarAmostra(MeioDia(10))], null, 0, CancellationToken.None);
            await File.WriteAllTextAsync(Path.Combine(_diretorioUso, "outro.jsonl"), string.Empty);
            await File.WriteAllTextAsync(Path.Combine(_diretorioUso, "uso-invalido.jsonl"), string.Empty);

            var dias = _sut.ListarDias();

            dias.Should().ContainSingle().Which.Should().Be(dia);
        }
    }

    public class LerDiaAsync : UsoJsonServiceTests
    {
        [Fact]
        public async Task Deve_retornar_lista_vazia_quando_arquivo_do_dia_nao_existe()
        {
            var amostras = await _sut.LerDiaAsync(new DateOnly(2026, 3, 10), CancellationToken.None);

            amostras.Should().BeEmpty();
        }

        [Fact]
        public async Task Deve_ignorar_linha_invalida_e_ler_as_demais()
        {
            var dia = new DateOnly(2026, 3, 10);
            Directory.CreateDirectory(_diretorioUso);
            await File.WriteAllTextAsync(
                CaminhoArquivo(dia),
                LinhaSemMaquina + "\n{ isso nao eh json\n" + LinhaSemMaquina + "\n");

            var amostras = await _sut.LerDiaAsync(dia, CancellationToken.None);

            amostras.Should().HaveCount(2);
        }

        [Fact]
        public async Task Deve_ler_ram_livre_legado_quando_linha_antiga_nao_tem_ram_usada()
        {
            var dia = new DateOnly(2026, 3, 10);
            Directory.CreateDirectory(_diretorioUso);
            await File.WriteAllTextAsync(CaminhoArquivo(dia), LinhaLegada + "\n");

            var amostras = await _sut.LerDiaAsync(dia, CancellationToken.None);

            amostras.Should().ContainSingle();
            amostras[0].RamLivreBytes.Should().Be(8_000_000_000);
            amostras[0].RamUsadaBytes.Should().BeNull();
        }
    }

    public class VerificarRetencao : UsoJsonServiceTests
    {
        [Fact]
        public async Task Deve_apagar_arquivo_antigo_e_manter_o_de_hoje_quando_retencao_de_um_dia()
        {
            Directory.CreateDirectory(_diretorioUso);
            var hoje = DateOnly.FromDateTime(DateTime.Now);
            var antigo = hoje.AddDays(-3);
            await File.WriteAllTextAsync(CaminhoArquivo(antigo), LinhaSemMaquina + "\n");

            await _sut.GravarLoteAsync([CriarAmostra(DateTimeOffset.Now)], CriarMaquina(), 1, CancellationToken.None);

            File.Exists(CaminhoArquivo(antigo)).Should().BeFalse();
            File.Exists(CaminhoArquivo(hoje)).Should().BeTrue();
        }

        [Fact]
        public async Task Nao_deve_apagar_arquivo_antigo_quando_retencao_desativada()
        {
            Directory.CreateDirectory(_diretorioUso);
            var antigo = DateOnly.FromDateTime(DateTime.Now).AddDays(-3);
            await File.WriteAllTextAsync(CaminhoArquivo(antigo), LinhaSemMaquina + "\n");

            await _sut.GravarLoteAsync([CriarAmostra(DateTimeOffset.Now)], CriarMaquina(), 0, CancellationToken.None);

            File.Exists(CaminhoArquivo(antigo)).Should().BeTrue();
        }
    }

    public class DiretorioRelatorios : UsoJsonServiceTests
    {
        [Fact]
        public void Deve_ficar_dentro_do_diretorio_de_uso()
        {
            _sut.DiretorioRelatorios.Should().Be(Path.Combine(_diretorioUso, "relatorios"));
        }
    }
}

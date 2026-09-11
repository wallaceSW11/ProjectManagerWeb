using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class LinuxDiscoIOColetorTests : IDisposable
{
    private readonly string _tempPath;
    private readonly string _caminhoDiskstats;
    private readonly string _caminhoBlock;

    public LinuxDiscoIOColetorTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempPath);
        _caminhoDiskstats = Path.Combine(_tempPath, "diskstats");
        _caminhoBlock = Path.Combine(_tempPath, "block");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private LinuxDiscoIOColetor CriarColetor() =>
        new(_caminhoDiskstats, _caminhoBlock);

    private void CriarDispositivo(string nome) =>
        Directory.CreateDirectory(Path.Combine(_caminhoBlock, nome));

    private void EscreverDiskstats(string conteudo) =>
        File.WriteAllText(_caminhoDiskstats, conteudo);

    private static string Linha(
        string nome,
        long leituras,
        long msLeitura,
        long setoresLidos,
        long setoresEscritos,
        long msOcupado) =>
        $"8 0 {nome} {leituras} 0 {setoresLidos} {msLeitura} 0 0 {setoresEscritos} 0 0 {msOcupado} 0";

    public class ObterMetricas : LinuxDiscoIOColetorTests
    {
        [Fact]
        public void Deve_retornar_nulos_na_primeira_amostra()
        {
            CriarDispositivo("sda");
            EscreverDiskstats(Linha("sda", 10, 100, 0, 0, 0));
            var coletor = CriarColetor();

            var (leitura, escrita, atividade, latencia) = coletor.ObterMetricas();

            leitura.Should().BeNull();
            escrita.Should().BeNull();
            atividade.Should().BeNull();
            latencia.Should().BeNull();
        }

        [Fact]
        public void Deve_calcular_latencia_de_leitura_por_delta()
        {
            CriarDispositivo("sda");
            EscreverDiskstats(Linha("sda", 10, 100, 0, 0, 0));
            var coletor = CriarColetor();
            coletor.ObterMetricas();

            EscreverDiskstats(Linha("sda", 20, 300, 0, 0, 0));

            var (_, _, _, latencia) = coletor.ObterMetricas();

            latencia.Should().NotBeNull();
            latencia!.Value.Should().BeApproximately(20.0, 0.001);
        }

        [Fact]
        public void Deve_retornar_latencia_nula_quando_nao_ha_delta_de_leituras()
        {
            CriarDispositivo("sda");
            EscreverDiskstats(Linha("sda", 10, 100, 0, 0, 0));
            var coletor = CriarColetor();
            coletor.ObterMetricas();

            EscreverDiskstats(Linha("sda", 10, 300, 0, 0, 0));

            var (_, _, _, latencia) = coletor.ObterMetricas();

            latencia.Should().BeNull();
        }

        [Fact]
        public void Deve_ignorar_dispositivo_ausente_em_sys_block()
        {
            CriarDispositivo("sda");
            EscreverDiskstats(
                Linha("sda", 10, 100, 0, 0, 0) + "\n"
                + Linha("loop0", 1000, 5000, 0, 0, 0));
            var coletor = CriarColetor();
            coletor.ObterMetricas();

            EscreverDiskstats(
                Linha("sda", 20, 300, 0, 0, 0) + "\n"
                + Linha("loop0", 5000, 10000, 0, 0, 0));

            var (_, _, _, latencia) = coletor.ObterMetricas();

            latencia.Should().NotBeNull();
            latencia!.Value.Should().BeApproximately(20.0, 0.001);
        }

        [Fact]
        public void Deve_limitar_atividade_entre_0_e_100()
        {
            CriarDispositivo("sda");
            EscreverDiskstats(Linha("sda", 10, 100, 0, 0, 0));
            var coletor = CriarColetor();
            coletor.ObterMetricas();

            EscreverDiskstats(Linha("sda", 20, 300, 0, 0, 10_000_000));

            var (_, _, atividade, _) = coletor.ObterMetricas();

            atividade.Should().NotBeNull();
            atividade!.Value.Should().BeInRange(0.0, 100.0);
        }

        [Fact]
        public void Deve_calcular_leitura_e_escrita_por_segundo_nao_nulas()
        {
            CriarDispositivo("sda");
            EscreverDiskstats(Linha("sda", 10, 100, 100, 200, 0));
            var coletor = CriarColetor();
            coletor.ObterMetricas();

            EscreverDiskstats(Linha("sda", 20, 300, 1100, 2200, 0));

            var (leitura, escrita, _, _) = coletor.ObterMetricas();

            leitura.Should().NotBeNull();
            escrita.Should().NotBeNull();
            leitura!.Value.Should().BeGreaterThanOrEqualTo(0);
            escrita!.Value.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public void Deve_retornar_nulos_sem_excecao_quando_diskstats_nao_existe()
        {
            var coletor = CriarColetor();

            var (leitura, escrita, atividade, latencia) = coletor.ObterMetricas();

            leitura.Should().BeNull();
            escrita.Should().BeNull();
            atividade.Should().BeNull();
            latencia.Should().BeNull();

            Action segundaAmostra = () => coletor.ObterMetricas();

            segundaAmostra.Should().NotThrow();
        }
    }
}

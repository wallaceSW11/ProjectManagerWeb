using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class LinuxRedeColetorTests : IDisposable
{
    private const string Cabecalho =
        "Inter-|   Receive                                                |  Transmit\n"
        + " face |bytes    packets errs drop fifo frame compressed multicast|bytes    packets errs drop fifo colls carrier compressed\n";

    private readonly string _caminhoNetDev;
    private readonly string _tempPath;

    public LinuxRedeColetorTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempPath);
        _caminhoNetDev = Path.Combine(_tempPath, "net-dev");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private void EscreverNetDev(string conteudo) =>
        File.WriteAllText(_caminhoNetDev, conteudo);

    private static string Linha(string face, long recebido, long transmitido) =>
        $" {face}: {recebido}    0 0 0 0 0 0 0   {transmitido}    0 0 0 0 0 0 0\n";

    public class ObterBytesPorSegundo : LinuxRedeColetorTests
    {
        [Fact]
        public void Deve_retornar_nulo_na_primeira_amostra()
        {
            EscreverNetDev(Cabecalho + Linha("lo", 1000, 2000) + Linha("enp1s0", 5000, 7000));
            var coletor = new LinuxRedeColetor(_caminhoNetDev);

            var (download, upload) = coletor.ObterBytesPorSegundo();

            download.Should().BeNull();
            upload.Should().BeNull();
        }

        [Fact]
        public void Deve_calcular_delta_entre_amostras_ignorando_loopback()
        {
            EscreverNetDev(Cabecalho + Linha("lo", 999999, 888888) + Linha("enp1s0", 1000, 2000));
            var coletor = new LinuxRedeColetor(_caminhoNetDev);
            coletor.ObterBytesPorSegundo();

            EscreverNetDev(Cabecalho + Linha("lo", 111111, 777777) + Linha("enp1s0", 6000, 3000));

            var (download, upload) = coletor.ObterBytesPorSegundo();

            download.Should().NotBeNull();
            upload.Should().NotBeNull();
            download!.Value.Should().BeGreaterThan(upload!.Value);
            (download.Value / (double)upload.Value).Should().BeApproximately(5.0, 0.1);
        }

        [Fact]
        public void Deve_somar_todas_as_interfaces_nao_loopback()
        {
            EscreverNetDev(Cabecalho + Linha("enp1s0", 1000, 2000));
            var coletor = new LinuxRedeColetor(_caminhoNetDev);
            coletor.ObterBytesPorSegundo();

            EscreverNetDev(Cabecalho + Linha("enp1s0", 2000, 4000) + Linha("eth1", 2000, 1000));

            var (download, upload) = coletor.ObterBytesPorSegundo();

            download.Should().NotBeNull();
            upload.Should().NotBeNull();
            (download!.Value / (double)upload!.Value).Should().BeApproximately(1.0, 0.05);
        }

        [Fact]
        public void Deve_retornar_nulo_sem_excecao_quando_arquivo_nao_existe()
        {
            var coletor = new LinuxRedeColetor(_caminhoNetDev);

            var (download, upload) = coletor.ObterBytesPorSegundo();

            download.Should().BeNull();
            upload.Should().BeNull();
        }

        [Fact]
        public void Deve_ignorar_linhas_malformadas_e_continuar_leitura()
        {
            EscreverNetDev(Cabecalho + Linha("enp1s0", 1000, 2000));
            var coletor = new LinuxRedeColetor(_caminhoNetDev);
            coletor.ObterBytesPorSegundo();

            EscreverNetDev(Cabecalho
                + " eth0: 999 1 2 3\n"
                + " linha-sem-dois-pontos\n"
                + Linha("enp1s0", 3000, 5000));

            var (download, upload) = coletor.ObterBytesPorSegundo();

            download.Should().NotBeNull();
            upload.Should().NotBeNull();
            (download!.Value / (double)upload!.Value).Should().BeApproximately(0.6667, 0.02);
        }
    }
}

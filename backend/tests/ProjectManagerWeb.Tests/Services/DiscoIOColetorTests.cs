using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class DiscoIOColetorTests
{
    private readonly IDiscoIOColetor _coletorPlataforma = Substitute.For<IDiscoIOColetor>();
    private readonly DiscoIOColetor _sut;

    public DiscoIOColetorTests()
    {
        _sut = new DiscoIOColetor(_coletorPlataforma);
    }

    public class ColetarAsync : DiscoIOColetorTests
    {
        [Fact]
        public async Task Deve_retornar_snapshot_com_metricas_de_io_quando_coletor_plataforma_preenchido()
        {
            _coletorPlataforma.ObterMetricas()
                .Returns((
                    leituraBytesPorSegundo: 1024L,
                    escritaBytesPorSegundo: 512L,
                    atividadePercentual: 75.5,
                    latenciaLeituraMs: 2.5));

            var snapshot = await _sut.ColetarAsync(CancellationToken.None);

            snapshot.DiscoLeituraBytesPorSegundo.Should().Be(1024);
            snapshot.DiscoEscritaBytesPorSegundo.Should().Be(512);
            snapshot.DiscoAtividadePercentual.Should().Be(75.5);
            snapshot.DiscoLatenciaLeituraMs.Should().Be(2.5);
        }

        [Fact]
        public async Task Deve_retornar_snapshot_com_demais_campos_nulos()
        {
            _coletorPlataforma.ObterMetricas()
                .Returns((
                    leituraBytesPorSegundo: 1024L,
                    escritaBytesPorSegundo: 512L,
                    atividadePercentual: 75.5,
                    latenciaLeituraMs: 2.5));

            var snapshot = await _sut.ColetarAsync(CancellationToken.None);

            snapshot.CpuPercentual.Should().BeNull();
            snapshot.RamTotalBytes.Should().BeNull();
            snapshot.RamUsadaBytes.Should().BeNull();
            snapshot.DiscoPercentual.Should().BeNull();
            snapshot.DiscoTotalBytes.Should().BeNull();
            snapshot.DiscoDisponivelBytes.Should().BeNull();
            snapshot.DiscoUsadaBytes.Should().BeNull();
            snapshot.CpuNome.Should().BeNull();
            snapshot.CpuFrequenciaMhz.Should().BeNull();
            snapshot.CpuTemperaturaCelsius.Should().BeNull();
            snapshot.RamVelocidadeMhz.Should().BeNull();
            snapshot.DiscoTemperaturaCelsius.Should().BeNull();
            snapshot.SwapTotalBytes.Should().BeNull();
            snapshot.SwapUsadaBytes.Should().BeNull();
            snapshot.RedeDownloadBytesPorSegundo.Should().BeNull();
            snapshot.RedeUploadBytesPorSegundo.Should().BeNull();
        }

        [Fact]
        public async Task Deve_retornar_campos_de_io_nulos_quando_coletor_plataforma_retorna_nulos()
        {
            _coletorPlataforma.ObterMetricas()
                .Returns((
                    leituraBytesPorSegundo: (long?)null,
                    escritaBytesPorSegundo: (long?)null,
                    atividadePercentual: (double?)null,
                    latenciaLeituraMs: (double?)null));

            var snapshot = await _sut.ColetarAsync(CancellationToken.None);

            snapshot.DiscoLeituraBytesPorSegundo.Should().BeNull();
            snapshot.DiscoEscritaBytesPorSegundo.Should().BeNull();
            snapshot.DiscoAtividadePercentual.Should().BeNull();
            snapshot.DiscoLatenciaLeituraMs.Should().BeNull();
        }

        [Fact]
        public async Task Deve_retornar_plataforma_conforme_sistema_operacional()
        {
            var snapshot = await _sut.ColetarAsync(CancellationToken.None);

            snapshot.Plataforma.Should().Be(OperatingSystem.IsWindows() ? "windows" : "linux");
        }
    }
}

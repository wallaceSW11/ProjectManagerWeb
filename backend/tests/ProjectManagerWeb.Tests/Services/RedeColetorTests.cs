using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class RedeColetorTests
{
    private readonly IRedeColetor _coletorPlataforma = Substitute.For<IRedeColetor>();
    private readonly RedeColetor _sut;

    public RedeColetorTests()
    {
        _sut = new RedeColetor(_coletorPlataforma);
    }

    public class ColetarAsync : RedeColetorTests
    {
        [Fact]
        public async Task Deve_retornar_snapshot_com_bytes_de_rede_quando_coletor_plataforma_preenchido()
        {
            _coletorPlataforma.ObterBytesPorSegundo()
                .Returns((downloadBytesPorSegundo: 1024L, uploadBytesPorSegundo: 512L));

            var snapshot = await _sut.ColetarAsync(CancellationToken.None);

            snapshot.RedeDownloadBytesPorSegundo.Should().Be(1024);
            snapshot.RedeUploadBytesPorSegundo.Should().Be(512);
        }

        [Fact]
        public async Task Deve_retornar_snapshot_com_demais_campos_nulos()
        {
            _coletorPlataforma.ObterBytesPorSegundo()
                .Returns((downloadBytesPorSegundo: 1024L, uploadBytesPorSegundo: 512L));

            var snapshot = await _sut.ColetarAsync(CancellationToken.None);

            snapshot.CpuPercentual.Should().BeNull();
            snapshot.RamTotalBytes.Should().BeNull();
            snapshot.RamDisponivelBytes.Should().BeNull();
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
        }

        [Fact]
        public async Task Deve_retornar_campos_de_rede_nulos_quando_coletor_plataforma_retorna_nulos()
        {
            _coletorPlataforma.ObterBytesPorSegundo()
                .Returns((downloadBytesPorSegundo: (long?)null, uploadBytesPorSegundo: (long?)null));

            var snapshot = await _sut.ColetarAsync(CancellationToken.None);

            snapshot.RedeDownloadBytesPorSegundo.Should().BeNull();
            snapshot.RedeUploadBytesPorSegundo.Should().BeNull();
        }
    }
}

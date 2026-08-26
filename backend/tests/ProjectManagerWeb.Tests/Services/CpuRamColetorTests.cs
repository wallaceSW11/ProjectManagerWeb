using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class CpuRamColetorTests
{
    private readonly ICpuRamColetor _coletorPlataforma = Substitute.For<ICpuRamColetor>();
    private readonly CpuRamColetor _sut;

    public CpuRamColetorTests()
    {
        _sut = new CpuRamColetor(_coletorPlataforma);
    }

    public class ColetarAsync : CpuRamColetorTests
    {
        [Fact]
        public async Task Deve_retornar_snapshot_com_campos_de_cpu_e_ram_quando_coletor_plataforma_preenchido()
        {
            _coletorPlataforma.ObterSistemaOperacional().Returns("Linux 6.8");
            _coletorPlataforma.ObterCpuPercentual().Returns(42.5);
            _coletorPlataforma.ObterMemoria().Returns((total: 17179869184L, disponivel: 8589934592L));
            _coletorPlataforma.ObterCpuNome().Returns("AMD Ryzen 7 5800X");
            _coletorPlataforma.ObterCpuFrequenciaMhz().Returns(3800.0);
            _coletorPlataforma.ObterCpuTemperaturaCelsius().Returns(62.5);
            _coletorPlataforma.ObterRamVelocidadeMhz().Returns(3600.0);

            var snapshot = await _sut.ColetarAsync(CancellationToken.None);

            snapshot.SistemaOperacional.Should().Be("Linux 6.8");
            snapshot.CpuPercentual.Should().Be(42.5);
            snapshot.RamTotalBytes.Should().Be(17179869184);
            snapshot.RamDisponivelBytes.Should().Be(8589934592);
            snapshot.RamUsadaBytes.Should().Be(8589934592);
            snapshot.CpuNome.Should().Be("AMD Ryzen 7 5800X");
            snapshot.CpuFrequenciaMhz.Should().Be(3800.0);
            snapshot.CpuTemperaturaCelsius.Should().Be(62.5);
            snapshot.RamVelocidadeMhz.Should().Be(3600.0);
        }

        [Fact]
        public async Task Deve_retornar_campos_de_ram_nulos_quando_memoria_total_eh_zero()
        {
            _coletorPlataforma.ObterMemoria().Returns((total: 0L, disponivel: 0L));
            _coletorPlataforma.ObterCpuPercentual().Returns(15.0);

            var snapshot = await _sut.ColetarAsync(CancellationToken.None);

            snapshot.RamTotalBytes.Should().BeNull();
            snapshot.RamDisponivelBytes.Should().BeNull();
            snapshot.RamUsadaBytes.Should().BeNull();
            snapshot.CpuPercentual.Should().Be(15.0);
        }

        [Fact]
        public async Task Deve_definir_plataforma_conforme_sistema_operacional_atual()
        {
            var snapshot = await _sut.ColetarAsync(CancellationToken.None);

            snapshot.Plataforma.Should().Be(OperatingSystem.IsWindows() ? "windows" : "linux");
        }

        [Fact]
        public async Task Deve_retornar_snapshot_com_campos_nulos_quando_coletor_plataforma_nao_informado()
        {
            var snapshot = await _sut.ColetarAsync(CancellationToken.None);

            snapshot.CpuPercentual.Should().BeNull();
            snapshot.RamTotalBytes.Should().BeNull();
            snapshot.RamDisponivelBytes.Should().BeNull();
            snapshot.RamUsadaBytes.Should().BeNull();
            snapshot.CpuNome.Should().BeEmpty();
            snapshot.CpuFrequenciaMhz.Should().BeNull();
            snapshot.CpuTemperaturaCelsius.Should().BeNull();
            snapshot.RamVelocidadeMhz.Should().BeNull();
        }
    }
}

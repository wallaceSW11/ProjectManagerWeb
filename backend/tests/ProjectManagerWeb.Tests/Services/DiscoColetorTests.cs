using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class DiscoColetorTests
{
    public class ColetarAsync : DiscoColetorTests
    {
        [Fact]
        public async Task Deve_retornar_snapshot_com_disco_do_diretorio_atual()
        {
            var sut = new DiscoColetor();

            var snapshot = await sut.ColetarAsync(CancellationToken.None);

            snapshot.DiscoTotalBytes.Should().BeGreaterThan(0);
            snapshot.DiscoDisponivelBytes.Should().BeGreaterThanOrEqualTo(0);
            snapshot.DiscoUsadaBytes.Should().BeGreaterThanOrEqualTo(0);
            snapshot.DiscoPercentual.Should().BeInRange(0, 100);
        }

        [Fact]
        public async Task Deve_retornar_campos_de_cpu_e_ram_nulos_quando_coleta_apenas_disco()
        {
            var sut = new DiscoColetor();

            var snapshot = await sut.ColetarAsync(CancellationToken.None);

            snapshot.CpuPercentual.Should().BeNull();
            snapshot.RamTotalBytes.Should().BeNull();
            snapshot.RamDisponivelBytes.Should().BeNull();
            snapshot.RamUsadaBytes.Should().BeNull();
            snapshot.CpuNome.Should().BeNull();
            snapshot.CpuFrequenciaMhz.Should().BeNull();
            snapshot.CpuTemperaturaCelsius.Should().BeNull();
            snapshot.RamVelocidadeMhz.Should().BeNull();
        }

        [Fact]
        public async Task Deve_retornar_contadores_zerados_e_sistema_operacional_vazio()
        {
            var sut = new DiscoColetor();

            var snapshot = await sut.ColetarAsync(CancellationToken.None);

            snapshot.ClientesConectados.Should().Be(0);
            snapshot.ContadorSnapshots.Should().Be(0);
            snapshot.SistemaOperacional.Should().BeEmpty();
        }
    }
}

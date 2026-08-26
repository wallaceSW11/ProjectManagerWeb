using NSubstitute.ExceptionExtensions;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services.Monitoramento;
using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class ColetorCompostoTests
{
    private readonly IColetorMetricas _coletorA = Substitute.For<IColetorMetricas>();
    private readonly IColetorMetricas _coletorB = Substitute.For<IColetorMetricas>();

    private static MonitoramentoSnapshotDTO CriarSnapshot(
        string plataforma = "linux",
        string sistemaOperacional = "",
        double? cpuPercentual = null,
        long? ramTotalBytes = null,
        long? ramDisponivelBytes = null,
        long? ramUsadaBytes = null,
        double? discoPercentual = null,
        long? discoTotalBytes = null,
        long? discoDisponivelBytes = null,
        long? discoUsadaBytes = null,
        string? cpuNome = null,
        double? cpuFrequenciaMhz = null,
        double? cpuTemperaturaCelsius = null,
        double? ramVelocidadeMhz = null) =>
        new(
            DateTime.UtcNow,
            plataforma,
            0,
            0,
            sistemaOperacional,
            cpuPercentual,
            ramTotalBytes,
            ramDisponivelBytes,
            ramUsadaBytes,
            discoPercentual,
            discoTotalBytes,
            discoDisponivelBytes,
            discoUsadaBytes,
            cpuNome,
            cpuFrequenciaMhz,
            cpuTemperaturaCelsius,
            ramVelocidadeMhz);

    public class ColetarAsync : ColetorCompostoTests
    {
        [Fact]
        public async Task Deve_retornar_snapshot_do_coletor_quando_ha_apenas_um()
        {
            var esperado = CriarSnapshot(sistemaOperacional: "Ubuntu", cpuPercentual: 25.0, cpuNome: "Intel i7");
            _coletorA.ColetarAsync(Arg.Any<CancellationToken>()).Returns(esperado);
            var sut = new ColetorComposto([_coletorA]);

            var resultado = await sut.ColetarAsync(CancellationToken.None);

            resultado.Should().BeEquivalentTo(esperado);
        }

        [Fact]
        public async Task Deve_mesclar_campos_nao_nulos_quando_ha_varios_coletores()
        {
            _coletorA.ColetarAsync(Arg.Any<CancellationToken>()).Returns(CriarSnapshot(
                sistemaOperacional: "Ubuntu",
                cpuPercentual: 25.0,
                ramTotalBytes: 1000,
                ramDisponivelBytes: 400,
                ramUsadaBytes: 600,
                cpuNome: "Intel i7"));

            _coletorB.ColetarAsync(Arg.Any<CancellationToken>()).Returns(CriarSnapshot(
                plataforma: "",
                discoPercentual: 50.0,
                discoTotalBytes: 2000,
                discoDisponivelBytes: 1000,
                discoUsadaBytes: 1000,
                cpuFrequenciaMhz: 3600.0));

            var sut = new ColetorComposto([_coletorA, _coletorB]);

            var resultado = await sut.ColetarAsync(CancellationToken.None);

            resultado.Plataforma.Should().Be("linux");
            resultado.SistemaOperacional.Should().Be("Ubuntu");
            resultado.CpuPercentual.Should().Be(25.0);
            resultado.RamTotalBytes.Should().Be(1000);
            resultado.RamDisponivelBytes.Should().Be(400);
            resultado.RamUsadaBytes.Should().Be(600);
            resultado.CpuNome.Should().Be("Intel i7");
            resultado.DiscoPercentual.Should().Be(50.0);
            resultado.DiscoTotalBytes.Should().Be(2000);
            resultado.DiscoDisponivelBytes.Should().Be(1000);
            resultado.DiscoUsadaBytes.Should().Be(1000);
            resultado.CpuFrequenciaMhz.Should().Be(3600.0);
        }

        [Fact]
        public async Task Deve_sobrescrever_com_valor_nao_nulo_do_segundo_coletor()
        {
            _coletorA.ColetarAsync(Arg.Any<CancellationToken>()).Returns(CriarSnapshot(cpuPercentual: 10.0));
            _coletorB.ColetarAsync(Arg.Any<CancellationToken>()).Returns(CriarSnapshot(cpuPercentual: 30.0));
            var sut = new ColetorComposto([_coletorA, _coletorB]);

            var resultado = await sut.ColetarAsync(CancellationToken.None);

            resultado.CpuPercentual.Should().Be(30.0);
        }

        [Fact]
        public async Task Deve_manter_valor_do_primeiro_coletor_quando_segundo_tem_campo_nulo()
        {
            _coletorA.ColetarAsync(Arg.Any<CancellationToken>()).Returns(CriarSnapshot(cpuPercentual: 10.0, cpuNome: "Intel"));
            _coletorB.ColetarAsync(Arg.Any<CancellationToken>()).Returns(CriarSnapshot(cpuPercentual: null, cpuNome: null));
            var sut = new ColetorComposto([_coletorA, _coletorB]);

            var resultado = await sut.ColetarAsync(CancellationToken.None);

            resultado.CpuPercentual.Should().Be(10.0);
            resultado.CpuNome.Should().Be("Intel");
        }

        [Fact]
        public async Task Deve_lancar_excecao_quando_coletor_interno_falha()
        {
            _coletorA.ColetarAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("falha ao coletar"));
            var sut = new ColetorComposto([_coletorA]);

            var act = () => sut.ColetarAsync(CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("falha ao coletar");
        }

        [Fact]
        public async Task Deve_lancar_excecao_quando_lista_de_coletores_eh_vazia()
        {
            var sut = new ColetorComposto([]);

            var act = () => sut.ColetarAsync(CancellationToken.None);

            await act.Should().ThrowAsync<Exception>().WithMessage("*Nenhum coletor de métricas configurado*");
        }
    }
}

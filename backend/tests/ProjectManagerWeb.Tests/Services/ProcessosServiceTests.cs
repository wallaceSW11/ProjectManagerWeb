using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services.Monitoramento;
using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class ProcessosServiceTests
{
    private readonly IProcessosColetor _coletor = Substitute.For<IProcessosColetor>();
    private readonly ProcessosService _sut;

    public ProcessosServiceTests()
    {
        _sut = new ProcessosService(_coletor);
    }

    public class ObterTopAsync : ProcessosServiceTests
    {
        [Fact]
        public async Task Deve_retornar_processos_do_coletor_quando_tipo_eh_cpu()
        {
            var esperado = new List<ProcessoInfoDTO> { new("processo", 10.0, null) };
            _coletor.ColetarTopAsync("cpu", Arg.Any<CancellationToken>()).Returns(esperado);

            var resultado = await _sut.ObterTopAsync("cpu", CancellationToken.None);

            resultado.Should().BeEquivalentTo(esperado);
            await _coletor.Received(1).ColetarTopAsync("cpu", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Deve_retornar_processos_do_coletor_quando_tipo_eh_ram()
        {
            var esperado = new List<ProcessoInfoDTO> { new("processo", 5.0, 1024L) };
            _coletor.ColetarTopAsync("ram", Arg.Any<CancellationToken>()).Returns(esperado);

            var resultado = await _sut.ObterTopAsync("ram", CancellationToken.None);

            resultado.Should().BeEquivalentTo(esperado);
            await _coletor.Received(1).ColetarTopAsync("ram", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Deve_normalizar_tipo_com_maiusculas_e_espacos()
        {
            var esperado = new List<ProcessoInfoDTO> { new("processo", 10.0, null) };
            _coletor.ColetarTopAsync("cpu", Arg.Any<CancellationToken>()).Returns(esperado);

            var resultado = await _sut.ObterTopAsync(" CPU ", CancellationToken.None);

            resultado.Should().BeEquivalentTo(esperado);
            await _coletor.Received(1).ColetarTopAsync("cpu", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Deve_lancar_argument_exception_quando_tipo_eh_invalido()
        {
            var act = () => _sut.ObterTopAsync("disco", CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*'cpu' ou 'ram'*");
            await _coletor.DidNotReceive().ColetarTopAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }
}

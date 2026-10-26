using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProjectManagerWeb.src.Controllers;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;
using ProjectManagerWeb.src.Services.Monitoramento;
using ProjectManagerWeb.src.Services.Monitoramento.Uso;

namespace ProjectManagerWeb.Tests.Controllers;

public class UsoControllerTests : IDisposable
{
    private readonly string _tempPath;
    private readonly UsoController _sut;

    public UsoControllerTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempPath);

        _sut = new UsoController(new RelatorioUsoService(
            new UsoJsonService(Substitute.For<ILogger<UsoJsonService>>(), Path.Combine(_tempPath, "uso")),
            new ConfiguracaoService(Path.Combine(_tempPath, "Configuracao.json")),
            Substitute.For<IColetorMetricas>(),
            Substitute.For<ILogger<RelatorioUsoService>>(),
            _ => { }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    public class GerarRelatorio : UsoControllerTests
    {
        [Fact]
        public async Task Deve_retornar_bad_request_quando_nao_ha_registros_no_periodo()
        {
            var resultado = await _sut.GerarRelatorio(new RelatorioUsoRequestDTO());

            resultado.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}

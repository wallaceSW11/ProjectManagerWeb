using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services.Monitoramento.Uso;

namespace ProjectManagerWeb.src.Controllers
{
    [ApiController]
    [Route("api/uso")]
    public class UsoController(RelatorioUsoService relatorioUsoService) : ControllerBase
    {
        [HttpPost("relatorio")]
        public async Task<IActionResult> GerarRelatorio([FromBody] RelatorioUsoRequestDTO? request)
        {
            try
            {
                var resultado = await relatorioUsoService.GerarAsync(request ?? new RelatorioUsoRequestDTO(), CancellationToken.None);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

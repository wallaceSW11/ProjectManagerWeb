using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers
{
    [ApiController]
    [Route("api/configuracoes")]
    public class ConfiguracaoController(ConfiguracaoService configuracaoService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> ObterConfiguracao()
        {
            return Ok(await configuracaoService.ObterConfiguracaoAsync());
        }

        [HttpPost]
        public async Task<IActionResult> SalvarConfiguracao(ConfiguracaoRequestDTO configuracao)
        {
            await configuracaoService.SalvarConfiguracaoAsync(configuracao);
            return NoContent();
        }
    }
}
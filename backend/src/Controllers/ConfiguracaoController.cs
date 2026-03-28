using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers
{
    [ApiController]
    [Route("api/configuracoes")]
    public class ConfiguracaoController(ConfiguracaoService configuracaoService, RepositorioJsonService repositorioJsonService) : ControllerBase
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

        [HttpPut("perfis/{nomeAntigo}")]
        public async Task<IActionResult> RenomearPerfil(string nomeAntigo, [FromBody] string nomeNovo)
        {
            var sucesso = await configuracaoService.RenomearPerfilAsync(nomeAntigo, nomeNovo);
            if (!sucesso) return NotFound("Perfil não encontrado");

            await repositorioJsonService.RenomearPerfilVSCodeAsync(nomeAntigo, nomeNovo);
            return NoContent();
        }
    }
}
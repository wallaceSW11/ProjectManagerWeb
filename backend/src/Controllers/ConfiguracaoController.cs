using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Controllers
{
    [ApiController]
    [Route("api/configuracoes")]
    public class ConfiguracaoController(ConfiguracaoService configuracaoService, RepositorioJsonService repositorioJsonService, IShellProvider shellProvider) : ControllerBase
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
            shellProvider.RecarregarTerminal();
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

        [HttpPost("pastas-centralizadoras")]
        public async Task<IActionResult> AdicionarPastaCentralizadora([FromBody] string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return BadRequest("Nome é obrigatório");

            try
            {
                await configuracaoService.AdicionarPastaCentralizadoraAsync(nome);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("pastas-centralizadoras/{nomeAntigo}")]
        public async Task<IActionResult> RenomearPastaCentralizadora(string nomeAntigo, [FromBody] string nomeNovo)
        {
            if (string.IsNullOrWhiteSpace(nomeNovo))
                return BadRequest("Nome é obrigatório");

            try
            {
                await configuracaoService.RenomearPastaCentralizadoraAsync(nomeAntigo, nomeNovo);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("pastas-centralizadoras/{nome}")]
        public async Task<IActionResult> RemoverPastaCentralizadora(string nome)
        {
            try
            {
                await configuracaoService.RemoverPastaCentralizadoraAsync(nome);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
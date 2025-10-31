using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers
{
    [ApiController]
    [Route("api/sites-iis")]
    public class SiteIISController(SiteIISJsonService siteIISService, DeployIISService deployService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> ConsultarTodos()
        {
            var sites = await siteIISService.GetAllAsync();
            
            var response = sites.Select(s => new SiteIISDeployResponseDTO(
                s.Identificador,
                s.Titulo,
                s.Nome,
                s.PastaRaiz,
                s.Pastas?.Count ?? 0,
                s.PoolsAplicacao?.Count ?? 0
            )).ToList();

            return Ok(response);
        }

        [HttpGet("{identificador:guid}")]
        public async Task<IActionResult> ConsultarPorId(Guid identificador)
        {
            var site = await siteIISService.GetByIdAsync(identificador);
            
            if (site == null)
                return NotFound($"Site com ID {identificador} não encontrado.");

            return Ok(site);
        }

        [HttpPost]
        public async Task<IActionResult> Cadastrar([FromBody] SiteIISRequestDTO site)
        {
            if (site == null)
                return BadRequest("O corpo da requisição não pode ser nulo.");

            try
            {
                var cadastrado = await siteIISService.AddAsync(site);
                return Ok(cadastrado);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{identificador:guid}")]
        public async Task<IActionResult> Atualizar(Guid identificador, [FromBody] SiteIISRequestDTO siteAtualizado)
        {
            if (siteAtualizado == null)
                return BadRequest("O corpo da requisição não pode ser nulo.");

            var sucesso = await siteIISService.UpdateAsync(identificador, siteAtualizado);
            
            if (!sucesso)
                return NotFound($"Site com ID {identificador} não encontrado.");

            return NoContent();
        }

        [HttpDelete("{identificador:guid}")]
        public async Task<IActionResult> Excluir(Guid identificador)
        {
            var sucesso = await siteIISService.DeleteAsync(identificador);
            
            if (!sucesso)
                return NotFound($"Site com ID {identificador} não encontrado.");

            return NoContent();
        }

        [HttpPost("{identificador:guid}/atualizar")]
        public async Task<IActionResult> AtualizarSite(Guid identificador)
        {
            try
            {
                var resultado = await deployService.AtualizarSiteAsync(identificador);
                
                if (resultado.Sucesso)
                    return Ok(resultado);
                else
                    return StatusCode(500, resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AtualizarSiteResponseDTO(
                    false,
                    $"Erro ao processar atualização: {ex.Message}",
                    new List<string> { ex.ToString() }
                ));
            }
        }
    }
}

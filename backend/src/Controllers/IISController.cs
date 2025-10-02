using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

/// <summary>
/// Controller para gerenciar sites do IIS
/// </summary>
[ApiController]
[Route("api/iis")]
public class IISController(IISService iisService) : ControllerBase
{
    /// <summary>
    /// Lista todos os sites do IIS
    /// </summary>
    /// <returns>Lista de sites com nome, porta e status</returns>
    [HttpGet("sites")]
    public async Task<IActionResult> ListarSites()
    {
        try
        {
            var sites = await iisService.ListarSitesAsync();
            return Ok(sites);
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    /// <summary>
    /// Inicia um site do IIS
    /// </summary>
    /// <param name="nomeSite">Nome do site a ser iniciado</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("sites/{nomeSite}/iniciar")]
    public async Task<IActionResult> IniciarSite(string nomeSite)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nomeSite))
                return BadRequest(new { erro = "Nome do site é obrigatório" });

            var sucesso = await iisService.IniciarSiteAsync(nomeSite);
            
            if (sucesso)
                return Ok(new { mensagem = $"Site '{nomeSite}' iniciado com sucesso" });
            else
                return BadRequest(new { erro = $"Falha ao iniciar o site '{nomeSite}'" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    /// <summary>
    /// Para um site do IIS
    /// </summary>
    /// <param name="nomeSite">Nome do site a ser parado</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("sites/{nomeSite}/parar")]
    public async Task<IActionResult> PararSite(string nomeSite)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nomeSite))
                return BadRequest(new { erro = "Nome do site é obrigatório" });

            var sucesso = await iisService.PararSiteAsync(nomeSite);
            
            if (sucesso)
                return Ok(new { mensagem = $"Site '{nomeSite}' parado com sucesso" });
            else
                return BadRequest(new { erro = $"Falha ao parar o site '{nomeSite}'" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    /// <summary>
    /// Reinicia um site do IIS
    /// </summary>
    /// <param name="nomeSite">Nome do site a ser reiniciado</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("sites/{nomeSite}/reiniciar")]
    public async Task<IActionResult> ReiniciarSite(string nomeSite)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nomeSite))
                return BadRequest(new { erro = "Nome do site é obrigatório" });

            var sucesso = await iisService.ReiniciarSiteAsync(nomeSite);
            
            if (sucesso)
                return Ok(new { mensagem = $"Site '{nomeSite}' reiniciado com sucesso" });
            else
                return BadRequest(new { erro = $"Falha ao reiniciar o site '{nomeSite}'" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    /// <summary>
    /// Executa uma ação específica em um site (iniciar, parar, reiniciar)
    /// </summary>
    /// <param name="request">Request com nome do site e ação desejada</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("sites/acao")]
    public async Task<IActionResult> ExecutarAcaoSite([FromBody] AcaoSiteIISRequestDTO request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request == null)
                return BadRequest(new { erro = "Request não pode ser nulo" });

            if (string.IsNullOrWhiteSpace(request.NomeSite))
                return BadRequest(new { erro = "Nome do site é obrigatório" });

            if (string.IsNullOrWhiteSpace(request.Acao))
                return BadRequest(new { erro = "Ação é obrigatória" });

            var sucesso = await iisService.ExecutarAcaoSiteAsync(request);
            
            if (sucesso)
                return Ok(new { mensagem = $"Ação '{request.Acao}' executada com sucesso no site '{request.NomeSite}'" });
            else
                return BadRequest(new { erro = $"Falha ao executar ação '{request.Acao}' no site '{request.NomeSite}'" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}
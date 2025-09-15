using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/versao")]
public class VersaoController() : ControllerBase
{

    [HttpGet]
    public IActionResult Versao()
    {
        var service = new InformacaoBuildService();
        DateTime dataCompilacao = service.ObterDataCompilacao();
        return Ok(dataCompilacao.ToString("dd/MM/yyyy HH:mm:ss"));
    }
}
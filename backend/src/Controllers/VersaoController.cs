using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/versao")]
public class VersaoController(VersaoService versaoService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Versao() =>
        Ok(await versaoService.ObterVersaoAsync());

    [HttpGet("compilacao")]
    public IActionResult Compilacao()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var data = System.IO.File.GetLastWriteTime(assembly.Location);
        return Ok(data.ToString("dd/MM/yyyy HH:mm:ss"));
    }

    [HttpPost("atualizar-aplicacao")]
    public IActionResult AtualizarAplicacao()
    {
        VersaoService.AtualizarAplicacao();
        return Ok();
    }

    [HttpGet("features")]
    public IActionResult Features() =>
        Ok(new
        {
            Iis = OperatingSystem.IsWindows(),
            Deploy = OperatingSystem.IsWindows(),
            TerminalProfiles = true,
            Os = OperatingSystem.IsWindows() ? "windows" : "linux"
        });
}

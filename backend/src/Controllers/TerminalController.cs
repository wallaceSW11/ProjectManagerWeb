using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/terminal")]
public class TerminalController(TerminalService terminalService) : ControllerBase
{
    [HttpGet("perfis")]
    public IActionResult ObterPerfis()
    {
        var perfis = terminalService.ObterPerfis();
        return Ok(perfis);
    }
}

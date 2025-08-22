using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/pastas")]
public class PastaController(PastaService pastaService) : ControllerBase
{

  [HttpGet]
  public async Task<IActionResult> ObterTodas()
  {
    var pastas = await pastaService.ObterTodas();
    return Ok(pastas);
  }
}
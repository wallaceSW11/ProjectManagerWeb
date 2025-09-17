using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/comandos")]
public class ComandoController(ComandoService comandoService) : ControllerBase
{

  [HttpPost]
  public async Task<IActionResult> ExecutarComando(PastaRequestDTO pasta)
  {
    var comandos = await comandoService.ExecutarComando(pasta);
    return Ok(comandos);
  }

  [HttpPost("avulso")]
  public IActionResult ExecutarComandoAvulso(string comando)
  {
    var comandos = comandoService.ExecutarComandoAvulso(comando);
    return Ok(comandos);
  }

  [HttpPost("menu")]
  public async Task<IActionResult> ExecutarComandoMenu(MenuRequestDTO menu)
  {
    var comandos = await comandoService.ExecutarComandoMenu(menu);
    return Ok(comandos);
  }
}
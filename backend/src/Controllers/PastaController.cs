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

  [HttpPost]
  public async Task<IActionResult> Cadastrar([FromBody] PastaCadastroRequestDTO pastaCadastro)
  {
    if (!ModelState.IsValid)
      return BadRequest(ModelState);

    var pasta = await pastaService.Cadastrar(pastaCadastro);
    return CreatedAtAction(nameof(ObterTodas), new { id = pasta.Identificador }, pasta);
  }

  [HttpPut("indices")]
  public async Task<IActionResult> AtualizarIndices([FromBody] List<PastaIndiceRequestDTO> indices)
  {
    if (!ModelState.IsValid)
      return BadRequest(ModelState);

    await pastaService.AtualizarIndices(indices);
    return Ok();
  }
}
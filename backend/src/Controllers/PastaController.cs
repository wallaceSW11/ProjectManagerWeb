using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/pastas")]
public class PastaController(PastaService pastaService, ConfiguracaoService configuracaoService) : ControllerBase
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

  [HttpPatch("projetos/expandido")]
  public async Task<IActionResult> AtualizarExpandidoProjeto([FromBody] AtualizarExpandidoRequestDTO request)
  {
    if (!ModelState.IsValid)
      return BadRequest(ModelState);

    try
    {
      await pastaService.AtualizarExpandidoProjeto(request.PastaId, request.ProjetoId, request.Expandido);
      return Ok();
    }
    catch (Exception ex)
    {
      return BadRequest(ex.Message);
    }
  }

  [HttpGet("ocultas")]
  public async Task<IActionResult> ObterOcultas()
  {
    var ocultas = await configuracaoService.ObterDiretoriosOcultosAsync();
    return Ok(ocultas);
  }

  [HttpPost("ocultar")]
  public async Task<IActionResult> Ocultar([FromBody] DiretorioRequestDTO request)
  {
    if (string.IsNullOrWhiteSpace(request.Diretorio)) return BadRequest();
    await configuracaoService.OcultarDiretorioAsync(request.Diretorio);
    return Ok();
  }

  [HttpPost("restaurar")]
  public async Task<IActionResult> Restaurar([FromBody] DiretorioRequestDTO request)
  {
    if (string.IsNullOrWhiteSpace(request.Diretorio)) return BadRequest();
    await configuracaoService.RestaurarDiretorioAsync(request.Diretorio);
    return Ok();
  }

  [HttpDelete]
  public async Task<IActionResult> ExcluirDiretorio([FromBody] DiretorioRequestDTO request)
  {
    if (string.IsNullOrWhiteSpace(request.Diretorio)) return BadRequest();

    if (!Directory.Exists(request.Diretorio))
      return NotFound("Diretório não encontrado.");

    try
    {
      RemoverAtributosReadOnly(new DirectoryInfo(request.Diretorio));
      Directory.Delete(request.Diretorio, recursive: true);
      await pastaService.RemoverPorDiretorio(request.Diretorio);
      return Ok();
    }
    catch (Exception ex)
    {
      return BadRequest($"Erro ao excluir o diretório: {ex.Message}");
    }
  }

  private static void RemoverAtributosReadOnly(DirectoryInfo diretorio)
  {
    foreach (var arquivo in diretorio.GetFiles("*", SearchOption.AllDirectories))
      arquivo.Attributes = FileAttributes.Normal;

    foreach (var subDiretorio in diretorio.GetDirectories("*", SearchOption.AllDirectories))
      subDiretorio.Attributes = FileAttributes.Normal;
  }
}
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/clones")]
public class CloneController(CloneService cloneService, PastaService pastaService, RepositorioJsonService repositorioJsonService) : ControllerBase
{

    [HttpGet("verificar-branch")]
    public async Task<IActionResult> VerificarBranch([FromQuery] string url, [FromQuery] string branch)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(branch))
            return BadRequest("URL e branch são obrigatórios.");

        var existe = await cloneService.VerificarBranchExisteAsync(url, branch);
        return Ok(new { existe });
    }

    [HttpPost]
    public async Task<IActionResult> Clonar([FromBody] CloneRequestDTO novaClonagem)
    {
        try
        {
            await cloneService.Clonar(novaClonagem);
            var pastaCriada = await CriarPasta(novaClonagem);
            return Ok(pastaCriada);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private async Task<IActionResult> CriarPasta(CloneRequestDTO novaClonagem)
    {
        var repositorio = await repositorioJsonService.GetByIdAsync(novaClonagem.RepositorioId);
        var diretorioCompleto = novaClonagem.DiretorioRaiz + novaClonagem.Codigo + "_" + novaClonagem.Descricao.Replace(" ", "_");

        var pasta = new PastaCadastroRequestDTO
        (
            Guid.NewGuid(),
            diretorioCompleto,
            novaClonagem.Codigo,
            novaClonagem.Descricao,
            novaClonagem.Tipo,
            novaClonagem.Branch,
            repositorio?.Url ?? "",
            novaClonagem.RepositorioId
        );

        await pastaService.Cadastrar(pasta);
        return Ok(pasta);
    }
}



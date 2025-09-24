using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/clones")]
public class CloneController(CloneService cloneService, PastaService pastaService, RepositorioJsonService repositorioJsonService) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> Clonar([FromBody] CloneRequestDTO novaClonagem)
    {
        await cloneService.Clonar(novaClonagem);
        var pastaCriada = await CriarPasta(novaClonagem);
        return Ok(pastaCriada);
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



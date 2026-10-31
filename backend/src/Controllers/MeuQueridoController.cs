using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.Services.MeuQuerido;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/meu-querido")]
public class MeuQueridoController(
    IOpenCodeClient openCodeClient,
    ProjetoLocalService projetoLocalService) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> ObterStatus(CancellationToken ct) =>
        Ok(await openCodeClient.ObterStatusAsync(ct));

    [HttpGet("projetos")]
    public async Task<IActionResult> ObterProjetos() =>
        Ok(await projetoLocalService.ObterProjetosAsync());

    [HttpGet("projetos/{projetoId:guid}")]
    public async Task<IActionResult> ObterProjeto(Guid projetoId)
    {
        var projeto = await projetoLocalService.ObterProjetoAsync(projetoId);

        if (projeto is null) return NotFound("Projeto não encontrado");

        return Ok(projeto);
    }
}

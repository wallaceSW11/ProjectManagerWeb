using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/clones")]
public class CloneController(CloneService cloneService) : ControllerBase
{

    // POST: api/Clone
    [HttpPost]
    public async Task<IActionResult> Clonar([FromBody] CloneRequestDTO novaClonagem)
    {
        await cloneService.Clonar(novaClonagem);
        return Ok();
    }

    
}
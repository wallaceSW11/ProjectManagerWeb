using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/comandos")]
public class ComandoController(ComandoService comandoService) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> ExecutarComando([FromBody] PastaRequestDTO pasta)
    {
        var comandos = await comandoService.ExecutarComando(pasta);
        return Ok(comandos);
    }

    [HttpPost("avulso")]
    public IActionResult ExecutarComandoAvulso([FromBody] ComandoAvulsoRequestDTO comando)
    {
        var comandos = comandoService.ExecutarComandoAvulso(comando.Comando, comando.PerfilTerminal);
        return Ok(comandos);
    }

    [HttpPost("menu")]
    public async Task<IActionResult> ExecutarComandoMenu([FromBody] MenuRequestDTO menu)
    {
        try
        {
            var resultado = await comandoService.ExecutarComandoMenu(menu);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("abrir-pasta-ide")]
    public async Task<IActionResult> AbrirPastaIDE([FromBody] AbrirPastaIDERequestDTO request)
    {
        var resultado = await comandoService.AbrirPastaIDE(request);
        return Ok(resultado);
    }
}
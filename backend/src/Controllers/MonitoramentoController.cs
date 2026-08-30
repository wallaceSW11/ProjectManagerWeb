using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.Services.Monitoramento;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/monitoramento")]
public class MonitoramentoController(MonitoramentoService monitoramentoService, ProcessosService processosService) : ControllerBase
{
    [HttpGet("ws")]
    public async Task<IActionResult> WebSocket()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return BadRequest("WebSocket esperado");

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await monitoramentoService.HandleWebSocketAsync(webSocket);
        return new EmptyResult();
    }

    [HttpGet("processos/top/{tipo}")]
    public async Task<IActionResult> TopProcessos(string tipo, CancellationToken ct)
    {
        try
        {
            return Ok(await processosService.ObterTopAsync(tipo, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

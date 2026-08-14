using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.Services.Monitoramento;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/monitoramento")]
public class MonitoramentoController(MonitoramentoService monitoramentoService) : ControllerBase
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
}

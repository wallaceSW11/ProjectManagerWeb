using Microsoft.AspNetCore.Mvc;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.src.Controllers;

[ApiController]
[Route("api/jarvas")]
public class JarvasController(
    ILLMService llmService,
    JarvasContextoService contextoService,
    JarvasOrquestrador orquestrador,
    JarvasPreParser preParser) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<JarvasChatResponseDTO>> Chat([FromBody] JarvasChatRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Mensagem))
            return BadRequest(new JarvasChatResponseDTO("Mensagem vazia.", null, false, null));

        try
        {
            // 1. Tenta resolver sem LLM (rápido)
            var preResult = await preParser.TentarResolverAsync(request.Mensagem);
            if (preResult != null)
            {
                var (resposta, acoesExecutadas) = await orquestrador.ExecutarFilaAsync(preResult.Acoes);
                return Ok(new JarvasChatResponseDTO(resposta, acoesExecutadas, true, null));
            }

            // 2. Fallback: chama o LLM
            var promptSistema = await contextoService.MontarPromptSistemaAsync();
            var tools = contextoService.ObterTools();

            var historico = request.Historico ?? [];
            historico.Add(new JarvasMensagemDTO("user", request.Mensagem));

            var llmRequest = new LLMRequestDTO(promptSistema, historico, tools);
            var llmResposta = await llmService.EnviarMensagemAsync(llmRequest);

            var (respostaLLM, acaoExecutada) = await orquestrador.ExecutarAsync(llmResposta);
            var acoes = acaoExecutada != null ? new List<string> { acaoExecutada } : null;

            return Ok(new JarvasChatResponseDTO(respostaLLM, acoes, true, null));
        }
        catch (TimeoutException ex)
        {
            return Ok(new JarvasChatResponseDTO(ex.Message, null, false, "timeout"));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(new JarvasChatResponseDTO(
                "Eita, o JARVAS está dormindo. Verifique se o Ollama está rodando.",
                null, false, ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(new JarvasChatResponseDTO(
                "Algo deu errado por aqui. Tenta de novo.",
                null, false, ex.Message));
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult<bool>> Status()
    {
        var disponivel = await llmService.VerificarDisponibilidadeAsync();
        return Ok(disponivel);
    }
}

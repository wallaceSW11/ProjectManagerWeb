using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

/// <summary>
/// Abstração do provedor de LLM. Trocar de Ollama para outro provedor
/// = criar nova implementação + mudar uma linha no Program.cs.
/// </summary>
public interface ILLMService
{
    Task<LLMRespostaDTO> EnviarMensagemAsync(LLMRequestDTO request, CancellationToken ct = default);
    Task<bool> VerificarDisponibilidadeAsync();
    Task WarmUpAsync();
}

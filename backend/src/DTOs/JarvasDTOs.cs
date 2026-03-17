namespace ProjectManagerWeb.src.DTOs;

// --- Requisição/Resposta do endpoint /api/jarvas/chat ---

public sealed record JarvasChatRequestDTO(
    string Mensagem,
    List<JarvasMensagemDTO>? Historico
);

public sealed record JarvasMensagemDTO(
    string Role,     // "user" | "assistant"
    string Conteudo
);

public sealed record JarvasChatResponseDTO(
    string Resposta,
    List<string>? AcoesExecutadas,  // lista de tools chamadas em sequência
    bool Sucesso,
    string? ErroDetalhes
);

// --- DTOs internos: comunicação entre ILLMService e Orquestrador ---

public sealed record LLMRequestDTO(
    string PromptSistema,
    List<JarvasMensagemDTO> Historico,
    List<LLMToolDTO> Tools
);

public sealed record LLMRespostaDTO(
    bool EhToolCall,
    string? ToolNome,
    string? ToolArgumentosJson,
    string? TextoResposta
);

public sealed record LLMToolDTO(
    string Nome,
    string Descricao,
    object Parametros
);

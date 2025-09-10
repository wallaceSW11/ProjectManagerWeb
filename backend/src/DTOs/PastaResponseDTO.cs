namespace ProjectManagerWeb.src.DTOs;

public sealed record PastaResponseDTO(
    string Pasta,
    string Codigo,
    string Descricao,
    string Tipo,
    string Branch,
    string Git,
    Guid GitId,
    List<ProjetoDisponivelDTO> Projetos
);

public sealed record ProjetoDisponivelDTO(
    string Nome,
    List<string> Comandos
);

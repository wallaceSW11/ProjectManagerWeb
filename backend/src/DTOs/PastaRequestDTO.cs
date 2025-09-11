namespace ProjectManagerWeb.src.DTOs;

public sealed record PastaRequestDTO(
    string Diretorio,
    Guid GitId,
    List<ProjetoDisponivelDTO> Projetos
);
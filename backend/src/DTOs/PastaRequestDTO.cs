namespace ProjectManagerWeb.src.DTOs;

public sealed record PastaRequestDTO(
    string Diretorio,
    Guid RepositorioId,
    List<ProjetoDisponivelDTO> Projetos
);
namespace ProjectManagerWeb.src.DTOs;

public sealed record PastaResponseDTO(
    string Diretorio,
    string Codigo,
    string Descricao,
    string Tipo,
    string Branch,
    string Git,
    Guid RepositorioId,
    List<ProjetoDisponivelDTO> Projetos,
    List<MenuDTO>? Menus
);

public sealed record ProjetoDisponivelDTO(
    string Nome,
    List<string> Comandos
);

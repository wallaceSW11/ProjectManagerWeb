namespace ProjectManagerWeb.src.DTOs;

public sealed record PastaResponseDTO(
    string Diretorio,
    string Codigo,
    string Descricao,
    string Tipo,
    string Branch,
    string Git,
    Guid RepositorioId,
    Guid? PastaId,
    List<ProjetoDisponivelDTO> Projetos,
    List<MenuDTO>? Menus
);

public sealed record ProjetoDisponivelDTO(
    Guid Identificador,
    string Nome,
    List<string> Comandos,
    Guid? IdentificadorRepositorioAgregado = null
);

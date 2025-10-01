namespace ProjectManagerWeb.src.DTOs;

public sealed record PastaResponseDTO(
    string Diretorio,
    string Codigo,
    string Descricao,
    string Tipo,
    string Branch,
    string Git,
    Guid RepositorioId,
    Guid? Identificador,
    List<ProjetoDisponivelDTO> Projetos,
    List<MenuDTO>? Menus,
    int Index = 0
);

public sealed record ProjetoDisponivelDTO(
    Guid Identificador,
    string Nome,
    string NomeRepositorio,
    List<string> Comandos,
    Guid? IdentificadorRepositorioAgregado = null,
    string? ArquivoCoverage = "",
    string? Subdiretorio = ""
);

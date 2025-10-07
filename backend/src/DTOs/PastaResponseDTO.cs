using ProjectManagerWeb.src.Enuns;

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
    int? Indice = null
);

public sealed record ProjetoDisponivelDTO(
    Guid Identificador,
    string Nome,
    string NomeRepositorio,
    List<ETipoComando> Comandos,
    Guid? IdentificadorRepositorioAgregado = null,
    string? ArquivoCoverage = "",
    string? Subdiretorio = "",
    bool Expandido = false
);

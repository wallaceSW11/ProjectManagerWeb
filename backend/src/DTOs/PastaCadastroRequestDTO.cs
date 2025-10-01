namespace ProjectManagerWeb.src.DTOs;

public sealed record PastaCadastroRequestDTO(
    Guid Identificador,
    string Diretorio,
    string Codigo,
    string Descricao,
    string Tipo,
    string Branch,
    string Git,
    Guid RepositorioId,
    int Index = 0
);

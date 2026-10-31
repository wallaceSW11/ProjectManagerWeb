namespace ProjectManagerWeb.src.DTOs;

public sealed record ProjetoLocalDTO(
    Guid ProjetoId,
    string Codigo,
    string Descricao,
    string Repositorio,
    string Diretorio,
    string DiretorioTrabalho,
    string? BranchAtual
);

namespace ProjectManagerWeb.src.DTOs;

/// <summary>
/// Representa uma solicitação de clonagem ou uma tarefa de desenvolvimento.
/// </summary>
public sealed record CloneRequestDTO(
    string DiretorioRaiz,
    string Codigo,
    string Descricao,
    string Tipo,
    string Branch,
    Guid GitId,
    bool CriarBranchRemoto,
    bool BaixarAgregados,
    bool BaixarSomenteAgregados
);
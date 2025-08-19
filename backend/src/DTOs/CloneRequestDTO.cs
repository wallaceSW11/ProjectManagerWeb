namespace ProjectManagerWeb.src.DTOs;

/// <summary>
/// Representa uma solicitação de clonagem ou uma tarefa de desenvolvimento.
/// </summary>
public sealed record CloneRequestDTO(
    string DiretorioRaiz,
    string Codigo,
    string Descricao,
    string Tipo,
    Guid GitId, // Usamos Guid pois o valor parece ser um Identificador Único Global
    bool CriarBranchRemoto,
    bool BaixarAgregados,
    bool BaixarSomenteAgregados
);
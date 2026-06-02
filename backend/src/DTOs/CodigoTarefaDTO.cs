namespace ProjectManagerWeb.src.DTOs;

public sealed record CodigoTarefaDTO(
    Guid Identificador,
    string Iniciais,
    string BranchPrincipal,
    bool CriarBranchRemoto = false,
    bool ClonarAgregados = false,
    bool BaixarHistoricoCompleto = false,
    bool HabilitarTipos = false,
    List<string>? TiposHabilitados = null
);

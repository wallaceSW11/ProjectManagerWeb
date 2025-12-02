namespace ProjectManagerWeb.src.DTOs
{
    /// <summary>
    /// DTO que representa uma IDE (Ambiente de Desenvolvimento Integrado).
    /// </summary>
    /// <param name="Identificador">Identificador único da IDE.</param>
    /// <param name="Nome">Nome de exibição da IDE (ex: "VS Code", "Kiro").</param>
    /// <param name="ComandoParaExecutar">Comando shell para abrir a IDE (ex: "code .", "kiro .").</param>
    /// <param name="AceitaPerfilPersonalizado">Indica se a IDE suporta perfis personalizados (--profile).</param>
    public sealed record IDEDTO(
        Guid Identificador,
        string Nome,
        string ComandoParaExecutar,
        bool AceitaPerfilPersonalizado
    );
}

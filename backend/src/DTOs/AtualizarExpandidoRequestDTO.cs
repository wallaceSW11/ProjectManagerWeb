using System.ComponentModel.DataAnnotations;

namespace ProjectManagerWeb.src.DTOs;

/// <summary>
/// DTO para atualizar o status expandido de um projeto.
/// </summary>
/// <param name="PastaId">Identificador único da pasta.</param>
/// <param name="ProjetoId">Identificador único do projeto.</param>
/// <param name="Expandido">Indica se o projeto deve estar expandido (true) ou recolhido (false).</param>
public sealed record AtualizarExpandidoRequestDTO(
    [Required] Guid PastaId,
    [Required] Guid ProjetoId,
    [Required] bool Expandido
);
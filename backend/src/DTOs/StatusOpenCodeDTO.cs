namespace ProjectManagerWeb.src.DTOs;

public sealed record StatusOpenCodeDTO(
    bool Disponivel,
    string? Versao,
    string? Url,
    string? Motivo
);

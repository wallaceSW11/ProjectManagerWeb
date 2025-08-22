namespace ProjectManagerWeb.src.DTOs;

public sealed record PastaResponseDTO(
    string Caminho,
    string GitUrl,
    string Branch
);

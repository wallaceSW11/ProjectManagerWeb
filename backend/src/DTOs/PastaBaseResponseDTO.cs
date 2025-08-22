namespace ProjectManagerWeb.src.DTOs;

public sealed record PastaBaseResponseDTO(
    string Diretorio,
    string GitUrl,
    string Branch
);

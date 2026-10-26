namespace ProjectManagerWeb.src.DTOs;

public sealed record RelatorioUsoRequestDTO(
    string? DataInicio = null,
    string? DataFim = null
);

public sealed record RelatorioUsoResponseDTO(
    string Caminho,
    long TamanhoBytes,
    int Registros
);

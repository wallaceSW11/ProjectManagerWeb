namespace ProjectManagerWeb.src.DTOs;

public sealed record RelatorioUsoRequestDTO(
    string? Data = null
);

public sealed record RelatorioUsoResponseDTO(
    string Caminho,
    long TamanhoBytes,
    int Registros
);

public sealed record UsoDiaDTO(
    string Data,
    long TamanhoBytes
);

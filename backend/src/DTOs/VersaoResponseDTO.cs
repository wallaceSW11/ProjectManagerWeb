namespace ProjectManagerWeb.src.DTOs;

public sealed record VersaoResponseDTO(
    string VersaoAtual,
    string? VersaoNova,
    string? UrlRelease,
    string? UrlDownload
);

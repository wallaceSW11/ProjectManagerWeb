namespace ProjectManagerWeb.src.DTOs;

/// <summary>
/// DTO de resposta para informações de sites do IIS
/// </summary>
public sealed record SiteIISResponseDTO(
    string Nome,
    string Porta,
    string Status
);

/// <summary>
/// DTO de request para ações em sites do IIS
/// </summary>
public sealed record AcaoSiteIISRequestDTO(
    string NomeSite,
    string Acao
);
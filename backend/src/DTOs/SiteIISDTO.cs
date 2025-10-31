using System.ComponentModel.DataAnnotations;

namespace ProjectManagerWeb.src.DTOs;

/// <summary>
/// DTO principal para cadastro e atualização de sites IIS
/// </summary>
public sealed record SiteIISRequestDTO(
    Guid Identificador,
    [Required] string Titulo,
    [Required] string Nome,
    [Required] string PastaRaiz,
    List<PastaDeployDTO> Pastas,
    List<string> PoolsAplicacao
);

/// <summary>
/// DTO de resposta para listagem de sites IIS do Deploy
/// </summary>
public sealed record SiteIISDeployResponseDTO(
    Guid Identificador,
    string Titulo,
    string Nome,
    string PastaRaiz,
    int QuantidadePastas,
    int QuantidadePools
);

/// <summary>
/// DTO de resposta para informações de sites do IIS (gerenciamento)
/// </summary>
public sealed record SiteIISResponseDTO(
    string Nome,
    string Porta,
    string Status
);

/// <summary>
/// DTO que representa uma pasta a ser implantada no deploy
/// </summary>
public sealed record PastaDeployDTO(
    Guid Identificador,
    [Required] string DiretorioTrabalho,
    [Required] string ComandoPublish,
    [Required] string CaminhoOrigem,
    [Required] string CaminhoDestino,
    [Required] string NomePastaDestino
);

/// <summary>
/// DTO de resposta para o processo de atualização
/// </summary>
public sealed record AtualizarSiteResponseDTO(
    bool Sucesso,
    string Mensagem,
    List<string> LogCompleto
);

/// <summary>
/// DTO de request para ações em sites do IIS (usado pelo IISController)
/// </summary>
public sealed record AcaoSiteIISRequestDTO(
    [Required] string NomeSite,
    [Required] string Acao
);
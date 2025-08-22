namespace ProjectManagerWeb.src.DTOs;

public sealed record PastaResponseDTO(
    string Pasta,
    string Codigo,
    string Descricao,
    string Tipo,
    string Git,
    List<ProjetooDTO> Projetos
);

public sealed record ProjetooDTO(
    string Nome,
    List<string> Comandos
);

namespace ProjectManagerWeb.src.DTOs;

public sealed record AbrirPastaIDERequestDTO(
  string Diretorio,
  Guid IDEIdentificador
);

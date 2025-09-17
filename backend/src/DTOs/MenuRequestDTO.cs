namespace ProjectManagerWeb.src.DTOs;

public sealed record MenuRequestDTO(
  Guid RepositorioId,
  Guid ComandoId,
  string Diretorio
);

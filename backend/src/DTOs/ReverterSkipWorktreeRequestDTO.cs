namespace ProjectManagerWeb.src.DTOs;

public sealed record ReverterSkipWorktreeRequestDTO(
    string Diretorio,
    string? NomeRepositorio = null,
    string? Subdiretorio = null
);

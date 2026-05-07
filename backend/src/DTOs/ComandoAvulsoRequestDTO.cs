namespace ProjectManagerWeb.src.DTOs;

public sealed record ComandoAvulsoRequestDTO(string Comando, string? PerfilTerminal = null);
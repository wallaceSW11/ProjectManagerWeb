namespace ProjectManagerWeb.src.DTOs;

public sealed record ConfiguracaoRequestDTO
(
    string DiretorioRaiz = "C:\\tools\\git",
    List<string>? PerfisVSCode = default
);

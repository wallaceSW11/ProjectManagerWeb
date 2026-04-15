namespace ProjectManagerWeb.src.DTOs;

public sealed record ConfiguracaoRequestDTO
(
    string DiretorioRaiz = "C:\\tools\\git",
    List<PerfilVSCodeRequestDTO>? PerfisVSCode = default,
    List<string>? DiretoriosOcultos = default
);

public sealed record PerfilVSCodeRequestDTO(
    string Nome
);

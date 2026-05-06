using System.Text.Json.Serialization;

namespace ProjectManagerWeb.src.DTOs;

public sealed record ConfiguracaoRequestDTO
(
    string DiretorioRaiz = "C:\\tools\\git",
    List<PerfilVSCodeRequestDTO>? PerfisVSCode = default,
    List<string>? DiretoriosOcultos = default,
    [property: JsonPropertyName("clis")] List<CliRequestDTO>? CLIs = default
);

public sealed record PerfilVSCodeRequestDTO(
    string Nome
);

public sealed record CliRequestDTO(
    string Nome,
    string Comando
);

using System.Text.Json.Serialization;

namespace ProjectManagerWeb.src.DTOs;

public sealed record ConfiguracaoRequestDTO
(
    string? DiretorioRaiz = null,
    List<PerfilVSCodeRequestDTO>? PerfisVSCode = default,
    List<string>? DiretoriosOcultos = default,
    [property: JsonPropertyName("clis")] List<CliRequestDTO>? CLIs = default,
    string? TerminalLinux = "ptyxis",
    List<PastaCentralizadoraDTO>? PastasCentralizadoras = default,
    UsoRecursosDTO? UsoRecursos = default
)
{
    public string? DiretorioRaizEfetivo => DiretorioRaiz;
}

public sealed record UsoRecursosDTO(
    bool Ativo = false,
    double LimiarCpuPercentual = 80,
    int IntervaloRegistroSegundos = 5,
    int RetencaoDias = 30
);

public sealed record PerfilVSCodeRequestDTO(
    string Nome
);

public sealed record CliRequestDTO(
    string Nome,
    string Comando
);

public sealed record PastaCentralizadoraDTO(string Nome);

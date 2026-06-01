using System.Text.Json.Serialization;

namespace ProjectManagerWeb.src.DTOs;

public sealed record ConfiguracaoRequestDTO
(
    string? DiretorioRaiz = null,
    List<PerfilVSCodeRequestDTO>? PerfisVSCode = default,
    List<string>? DiretoriosOcultos = default,
    [property: JsonPropertyName("clis")] List<CliRequestDTO>? CLIs = default,
    string? TerminalLinux = "ptyxis"
)
{
    public string DiretorioRaizEfetivo => DiretorioRaiz
        ?? (OperatingSystem.IsWindows()
            ? @"C:\tools\git"
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "git"));
}

public sealed record PerfilVSCodeRequestDTO(
    string Nome
);

public sealed record CliRequestDTO(
    string Nome,
    string Comando
);

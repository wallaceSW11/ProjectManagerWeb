using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class ConfiguracaoService
{
    private static readonly string BasePath = PathHelper.BancoPath;

    private static readonly string FilePath =
        Path.Combine(BasePath, "Configuracao.json");

    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ConfiguracaoService()
    {
        if (!Directory.Exists(BasePath))
        {
            Directory.CreateDirectory(BasePath);
        }
    }

    /// <summary>
    /// Obtém o objeto de configuração do arquivo JSON.
    /// </summary>
    public async Task<ConfiguracaoRequestDTO> ObterConfiguracaoAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(FilePath))
            {
                return new ConfiguracaoRequestDTO();
            }

            var jsonString = await File.ReadAllTextAsync(FilePath);
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return new ConfiguracaoRequestDTO();
            }

            var config = JsonSerializer.Deserialize<ConfiguracaoRequestDTO>(jsonString, _jsonOptions);
            return config ?? new ConfiguracaoRequestDTO();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Salva (substitui) o objeto de configuração no arquivo JSON.
    /// </summary>
    public async Task SalvarConfiguracaoAsync(ConfiguracaoRequestDTO configuracao)
    {
        await _semaphore.WaitAsync();
        try
        {
            var jsonString = JsonSerializer.Serialize(configuracao, _jsonOptions);
            await File.WriteAllTextAsync(FilePath, jsonString);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> RenomearPerfilAsync(string nomeAntigo, string nomeNovo)
    {
        var configuracao = await ObterConfiguracaoAsync();

        var perfil = configuracao.PerfisVSCode?.FirstOrDefault(p =>
            p.Nome.Equals(nomeAntigo, StringComparison.OrdinalIgnoreCase));

        if (perfil is null) return false;

        var perfisAtualizados = configuracao.PerfisVSCode!
            .Select(p => p.Nome.Equals(nomeAntigo, StringComparison.OrdinalIgnoreCase)
                ? new PerfilVSCodeRequestDTO(nomeNovo)
                : p)
            .ToList();

        await SalvarConfiguracaoAsync(configuracao with { PerfisVSCode = perfisAtualizados });
        return true;
    }
}

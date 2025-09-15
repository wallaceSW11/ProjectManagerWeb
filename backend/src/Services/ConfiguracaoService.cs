using System.Text.Json;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

public class ConfiguracaoService
{
    private static readonly string BasePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "PMW",
            "Banco"
        );

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
}

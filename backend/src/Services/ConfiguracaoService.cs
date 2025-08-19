using System.Text.Json;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

public class ConfiguracaoService
{
    private const string FilePath = "Banco/Configuracao.json";
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        WriteIndented = true,
        // Adicionamos esta opção para garantir que o mapeamento de nomes funcione bem
        PropertyNameCaseInsensitive = true 
    };

    public ConfiguracaoService()
    {
        var directoryName = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
    }

    /// <summary>
    /// Obtém o objeto de configuração do arquivo JSON.
    /// </summary>
    /// <returns>O objeto de configuração ou um objeto padrão se o arquivo não existir.</returns>
    public async Task<ConfiguracaoRequestDTO> ObterConfiguracaoAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(FilePath))
            {
                // Retorna um objeto padrão se o arquivo de configuração ainda não foi criado
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
    /// <param name="configuracao">O objeto de configuração a ser salvo.</param>
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
using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class ConfiguracaoService
{
    private static readonly string BasePath = PathHelper.BancoPath;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly string _filePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ConfiguracaoService()
    {
        _filePath = Path.Combine(BasePath, "Configuracao.json");

        if (!Directory.Exists(BasePath))
            Directory.CreateDirectory(BasePath);
    }

    internal ConfiguracaoService(string test_filePath) : this()
    {
        _filePath = test_filePath;
    }

    /// <summary>
    /// Obtém o objeto de configuração do arquivo JSON.
    /// </summary>
    public async Task<ConfiguracaoRequestDTO> ObterConfiguracaoAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
            {
                return new ConfiguracaoRequestDTO();
            }

            var jsonString = await File.ReadAllTextAsync(_filePath);
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
            await File.WriteAllTextAsync(_filePath, jsonString);
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

    public async Task<List<string>> ObterDiretoriosOcultosAsync()
    {
        var configuracao = await ObterConfiguracaoAsync();
        return configuracao.DiretoriosOcultos ?? [];
    }

    public async Task OcultarDiretorioAsync(string diretorio)
    {
        var configuracao = await ObterConfiguracaoAsync();
        var ocultos = configuracao.DiretoriosOcultos ?? [];

        if (ocultos.Contains(diretorio, StringComparer.OrdinalIgnoreCase)) return;

        await SalvarConfiguracaoAsync(configuracao with
        {
            DiretoriosOcultos = [.. ocultos, diretorio]
        });
    }

    public async Task RestaurarDiretorioAsync(string diretorio)
    {
        var configuracao = await ObterConfiguracaoAsync();
        var ocultos = configuracao.DiretoriosOcultos ?? [];

        var atualizados = ocultos
            .Where(d => !d.Equals(diretorio, StringComparison.OrdinalIgnoreCase))
            .ToList();

        await SalvarConfiguracaoAsync(configuracao with { DiretoriosOcultos = atualizados });
    }

    public async Task AdicionarPastaCentralizadoraAsync(string nome)
    {
        var configuracao = await ObterConfiguracaoAsync();
        var pastas = configuracao.PastasCentralizadoras ?? [];

        if (pastas.Any(p => p.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase)))
            throw new Exception("Já existe uma pasta centralizadora com esse nome");

        await SalvarConfiguracaoAsync(configuracao with
        {
            PastasCentralizadoras = [.. pastas, new PastaCentralizadoraDTO(nome)]
        });
    }

    public async Task RenomearPastaCentralizadoraAsync(string nomeAntigo, string nomeNovo)
    {
        var configuracao = await ObterConfiguracaoAsync();
        var pastas = configuracao.PastasCentralizadoras ?? [];

        var lista = pastas
            .Select(p => p.Nome.Equals(nomeAntigo, StringComparison.OrdinalIgnoreCase)
                ? new PastaCentralizadoraDTO(nomeNovo)
                : p)
            .ToList();

        await SalvarConfiguracaoAsync(configuracao with { PastasCentralizadoras = lista });
    }

    public async Task RemoverPastaCentralizadoraAsync(string nome)
    {
        var configuracao = await ObterConfiguracaoAsync();
        var pastas = configuracao.PastasCentralizadoras ?? [];

        var atualizadas = pastas
            .Where(p => !p.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (atualizadas.Count == pastas.Count)
            throw new Exception("Pasta centralizadora não encontrada");

        await SalvarConfiguracaoAsync(configuracao with { PastasCentralizadoras = atualizadas });
    }
}

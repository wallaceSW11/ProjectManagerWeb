using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class CloneService
{
    private const string FilePath = "Banco\\pastas.json";
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly RepositorioJsonService _repositorioJson;

    public CloneService(RepositorioJsonService repositorioJson)
    {
        var directoryName = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        _repositorioJson = repositorioJson;
    }

    public async Task<bool> Clonar(CloneRequestDTO clone)
    {
        StringBuilder diretorioCompleto = new();
        StringBuilder comando = new();

        diretorioCompleto
            .Append(clone.DiretorioRaiz)
            .Append(clone.Codigo)
            .Append('_')
            .Append(clone.Descricao.Replace(" ", "_"));

        if (!Directory.Exists(diretorioCompleto.ToString()))
            Directory.CreateDirectory(diretorioCompleto.ToString());

        var gitPrincipal = await _repositorioJson.GetByIdAsync(clone.GitId);

        if (gitPrincipal is null)
        {
            return false;
            throw new Exception("Git não encontrado");
        }

        var regexPattern = @"/([^/]+)\.git$";
        var regex = new Regex(regexPattern);

        Match match = regex.Match(gitPrincipal.Url);

        var nomeGit = ObterNomeRepositorio(gitPrincipal.Url) ?? throw new Exception("Nome do repositório git não encontrado");

        comando
            .Append($"cd {diretorioCompleto}; ")
            .Append($"git clone {gitPrincipal.Url}; ")
            .Append($"cd {nomeGit}; ")
            .Append($"git checkout {(clone.CriarBranchRemoto ? "-b" : "")} ")
            .Append($"{clone.Tipo}/{clone.Codigo}");

        ShellExecute.ExecutarComando(comando.ToString());

        if (clone.BaixarAgregados)
        {
            diretorioCompleto.Clear();
            comando.Clear();

            diretorioCompleto
                .Append(clone.DiretorioRaiz)
                .Append(clone.Codigo)
                .Append("_Backend_")
                .Append(clone.Descricao.Replace(" ", "_"));

            if (!Directory.Exists(diretorioCompleto.ToString()))
                Directory.CreateDirectory(diretorioCompleto.ToString());

            gitPrincipal.Agregados?.ForEach(agregado =>
            {
                comando.Clear();

                nomeGit = ObterNomeRepositorio(agregado) ?? throw new Exception("Nome do repositório git não encontrado");
                comando
                    .Append($"cd {diretorioCompleto}; ")
                    .Append($"git clone {agregado}; ")
                    .Append($"cd {nomeGit}; ")
                    .Append($"git checkout {(clone.CriarBranchRemoto ? "-b" : "")} ")
                    .Append($"{clone.Tipo}/{clone.Codigo}");

                ShellExecute.ExecutarComando(comando.ToString());
            });
        }

        return true;
    }

    public async Task<List<CloneRequestDTO>> GetAllAsync()
    {
        return await LerListaDoArquivoAsync();
    }

    // public async Task<CloneRequestDTO?> GetByIdAsync(Guid id)
    // {
    //     var clonagens = await LerListaDoArquivoAsync();
    //     return clonagens.FirstOrDefault(c => c.Id == id);
    // }

    // public async Task<CloneRequestDTO> AddAsync(CloneRequestDTO novaClonagem)
    // {
    //     await _semaphore.WaitAsync();
    //     try
    //     {
    //         var clonagens = await LerListaDoArquivoAsync(locked: true);

    //         var clonagemParaAdicionar = novaClonagem with { Id = Guid.NewGuid() };

    //         clonagens.Add(clonagemParaAdicionar);
    //         await GravarListaNoArquivoAsync(clonagens, locked: true);

    //         return clonagemParaAdicionar;
    //     }
    //     finally
    //     {
    //         _semaphore.Release();
    //     }
    // }

    // public async Task<bool> UpdateAsync(Guid id, CloneRequestDTO clonagemAtualizada)
    // {
    //     await _semaphore.WaitAsync();
    //     try
    //     {
    //         var clonagens = await LerListaDoArquivoAsync(locked: true);
    //         var index = clonagens.FindIndex(c => c.Id == id);

    //         if (index == -1) return false;

    //         clonagens[index] = clonagemAtualizada with { Id = id };

    //         await GravarListaNoArquivoAsync(clonagens, locked: true);
    //         return true;
    //     }
    //     finally
    //     {
    //         _semaphore.Release();
    //     }
    // }

    // public async Task<bool> DeleteAsync(Guid id)
    // {
    //     await _semaphore.WaitAsync();
    //     try
    //     {
    //         var clonagens = await LerListaDoArquivoAsync(locked: true);
    //         var itemsRemovidos = clonagens.RemoveAll(c => c.Id == id);

    //         if (itemsRemovidos == 0) return false;

    //         await GravarListaNoArquivoAsync(clonagens, locked: true);
    //         return true;
    //     }
    //     finally
    //     {
    //         _semaphore.Release();
    //     }
    // }

    // --- MÉTODOS PRIVADOS DE ACESSO AO ARQUIVO ---

    private async Task<List<CloneRequestDTO>> LerListaDoArquivoAsync(bool locked = false)
    {
        if (!locked) await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(FilePath)) return new List<CloneRequestDTO>();
            var jsonString = await File.ReadAllTextAsync(FilePath);
            if (string.IsNullOrWhiteSpace(jsonString)) return new List<CloneRequestDTO>();
            return JsonSerializer.Deserialize<List<CloneRequestDTO>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<CloneRequestDTO>();
        }
        finally
        {
            if (!locked) _semaphore.Release();
        }
    }

    private async Task GravarListaNoArquivoAsync(List<CloneRequestDTO> clonagens, bool locked = false)
    {
        if (!locked) await _semaphore.WaitAsync();
        try
        {
            var jsonString = JsonSerializer.Serialize(clonagens, _jsonOptions);
            await File.WriteAllTextAsync(FilePath, jsonString);
        }
        finally
        {
            if (!locked) _semaphore.Release();
        }
    }

    private static string ObterNomeRepositorio(string url)
    {
        var regexPattern = @"/([^/]+)\.git$";
        var regex = new Regex(regexPattern);

        Match match = regex.Match(url);

        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
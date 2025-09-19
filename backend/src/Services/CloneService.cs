using System.Text;
using System.Text.Json;
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

        var gitPrincipal = await _repositorioJson.GetByIdAsync(clone.RepositorioId);

        if (gitPrincipal is null)
        {
            return false;
            throw new Exception("Git não encontrado");
        }

        var nomeGit = RepositorioRequestDTO.ObterNomeRepositorio(gitPrincipal.Url) ?? throw new Exception("Nome do repositório git não encontrado");

        comando
            .Append($"cd {diretorioCompleto.ToString()}; ")
            .Append($"git clone {gitPrincipal.Url}; ")
            .Append($"cd {nomeGit}; ")
            .Append($"git checkout {clone.Branch}; ");

        if (clone.CriarBranchRemoto)
            if (clone.Tipo == "nenhum")
                comando.Append($" git checkout -b {clone.Codigo};");
            else
                comando.Append($" git checkout -b {clone.Tipo}/{clone.Codigo};");

        ShellExecute.ExecutarComando(comando.ToString());

        if (clone.BaixarAgregados)
        {
    
            gitPrincipal.Agregados?.ForEach(async identificadorAgredado =>
            {
                var agregado = await _repositorioJson.GetByIdAsync(identificadorAgredado);

                if (agregado is null) return;

                comando.Clear();

                nomeGit = RepositorioRequestDTO.ObterNomeRepositorio(agregado.Url) ?? throw new Exception("Nome do repositório git não encontrado");
                comando
                    .Append($"cd {diretorioCompleto.ToString()}; ")
                    .Append($"git clone {agregado.Url}; ")
                    .Append($"cd {nomeGit}; ")
                    .Append($"git checkout {clone.Branch}; ");

                if (clone.CriarBranchRemoto)
                    if (clone.Tipo == "nenhum")
                        comando.Append($" git checkout -b {clone.Codigo};");
                    else
                        comando.Append($" git checkout -b {clone.Tipo}/{clone.Codigo};");

                ShellExecute.ExecutarComando(comando.ToString());
            });
        }

        return true;
    }
}
using System.Text;
using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class CloneService
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly RepositorioJsonService _repositorioJson;

    public CloneService(RepositorioJsonService repositorioJson)
    {
        if (!Directory.Exists(PathHelper.BancoPath))
            Directory.CreateDirectory(PathHelper.BancoPath);

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

        comando
            .Append($"cd {diretorioCompleto.ToString()}; ")
            .Append($"{MontarComandoClone(clone.CriarBranchRemoto, clone.HistoricoCompleto, clone.Branch)} {gitPrincipal.Url}; ")
            .Append($"cd {gitPrincipal.Nome}; ");

        if (clone.CriarBranchRemoto)
        {
            comando.Append($" git checkout {clone.Branch} 2>$null; ");
            if (clone.Tipo == "nenhum")
                comando.Append($" git checkout -b {clone.Codigo};");
            else
                comando.Append($" git checkout -b {clone.Tipo}/{clone.Codigo};");
        }
        else
        {
            comando.Append($" git checkout {clone.Branch};");
        }

        ShellExecute.ExecutarComando(comando.ToString());

        if (clone.BaixarAgregados)
        {

            gitPrincipal.Agregados?.ForEach(async identificadorAgredado =>
            {
                var agregado = await _repositorioJson.GetByIdAsync(identificadorAgredado);

                if (agregado is null) return;

                comando.Clear();

                comando
                    .Append($"cd {diretorioCompleto.ToString()}; ")
                    .Append($"{MontarComandoClone(clone.CriarBranchRemoto, clone.HistoricoCompleto, clone.Branch)} {agregado.Url}; ")
                    .Append($"cd {agregado.Nome}; ");

                if (clone.CriarBranchRemoto)
                {
                    comando.Append($" git checkout {clone.Branch} 2>$null; ");
                    if (clone.Tipo == "nenhum")
                        comando.Append($" git checkout -b {clone.Codigo};");
                    else
                        comando.Append($" git checkout -b {clone.Tipo}/{clone.Codigo};");
                }
                else
                {
                    comando.Append($" git checkout {clone.Branch};");
                }

                ShellExecute.ExecutarComando(comando.ToString());
            });
        }

        return true;
    }

    private static string MontarComandoClone(bool criarBranchRemoto, bool historicoCompleto, string branch)
    {
        if (historicoCompleto) return "git clone";
        if (criarBranchRemoto && !EhBranchBase(branch)) return "git clone --depth 1 --no-single-branch";
        return criarBranchRemoto ? "git clone --depth 1" : "git clone --depth 1 --no-single-branch";
    }

    private static bool EhBranchBase(string branch) =>
        branch is "develop" or "dev" or "main" or "master";
}
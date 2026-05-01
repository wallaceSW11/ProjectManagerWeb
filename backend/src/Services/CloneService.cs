using System.Diagnostics;
using System.Text;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class CloneService
{
    private readonly RepositorioJsonService _repositorioJson;

    public CloneService(RepositorioJsonService repositorioJson)
    {
        if (!Directory.Exists(PathHelper.BancoPath))
            Directory.CreateDirectory(PathHelper.BancoPath);

        _repositorioJson = repositorioJson;
    }

    public async Task<bool> VerificarBranchExisteAsync(string url, string branch)
    {
        var resultado = await ExecutarComandoComRetornoAsync($"git ls-remote --heads \"{url}\" \"{branch}\"");
        return !string.IsNullOrWhiteSpace(resultado);
    }

    public async Task<string> DetectarBranchPrincipalAsync(string url)
    {
        var resultado = await ExecutarComandoComRetornoAsync($"git ls-remote --symref \"{url}\" HEAD");
        var linha = resultado
            .Split('\n')
            .FirstOrDefault(l => l.StartsWith("ref: refs/heads/"));

        if (linha is null) return string.Empty;

        return linha.Replace("ref: refs/heads/", "").Split('\t')[0].Trim();
    }

    public async Task<bool> Clonar(CloneRequestDTO clone)
    {
        StringBuilder diretorioCompleto = new();

        diretorioCompleto
            .Append(clone.DiretorioRaiz)
            .Append(clone.Codigo)
            .Append('_')
            .Append(clone.Descricao.Replace(" ", "_"));

        if (!Directory.Exists(diretorioCompleto.ToString()))
            Directory.CreateDirectory(diretorioCompleto.ToString());

        var gitPrincipal = await _repositorioJson.GetByIdAsync(clone.RepositorioId);

        if (gitPrincipal is null)
            return false;

        string branchPrincipalPrincipal = string.Empty;
        if (!EhBranchBase(clone.Branch))
        {
            var existe = await VerificarBranchExisteAsync(gitPrincipal.Url!, clone.Branch);
            if (!existe)
                throw new Exception($"Branch '{clone.Branch}' não encontrada no remote.");

            branchPrincipalPrincipal = await DetectarBranchPrincipalAsync(gitPrincipal.Url!);
        }

        var comandoPrincipal = MontarComandoClone(clone.HistoricoCompleto, clone.Branch);
        var scriptPrincipal = MontarScript(diretorioCompleto.ToString(), gitPrincipal.Url!, gitPrincipal.Nome, comandoPrincipal, clone, branchPrincipalPrincipal);
        ShellExecute.ExecutarComando(scriptPrincipal);

        if (clone.BaixarAgregados)
        {
            gitPrincipal.Agregados?.ForEach(async identificadorAgregado =>
            {
                var agregado = await _repositorioJson.GetByIdAsync(identificadorAgregado);
                if (agregado is null) return;

                var branchAgregado = clone.Branch;
                var branchPrincipalAgregado = string.Empty;

                if (!EhBranchBase(clone.Branch))
                {
                    var existeNoAgregado = await VerificarBranchExisteAsync(agregado.Url!, clone.Branch);
                    if (existeNoAgregado)
                    {
                        branchPrincipalAgregado = await DetectarBranchPrincipalAsync(agregado.Url!);
                    }
                    else
                    {
                        branchAgregado = string.Empty;
                    }
                }

                var comandoAgregado = MontarComandoClone(clone.HistoricoCompleto, branchAgregado);
                var scriptAgregado = MontarScript(diretorioCompleto.ToString(), agregado.Url!, agregado.Nome, comandoAgregado, clone with { Branch = branchAgregado }, branchPrincipalAgregado);
                ShellExecute.ExecutarComando(scriptAgregado);
            });
        }

        return true;
    }

    private static string MontarScript(string diretorioCompleto, string url, string nomeRepo, string comandoClone, CloneRequestDTO clone, string branchPrincipal = "")
    {
        StringBuilder script = new();

        script
            .Append($"cd \"{diretorioCompleto}\"; ")
            .Append($"{comandoClone} {url}; ")
            .Append($"cd \"{nomeRepo}\"; ");

        if (!string.IsNullOrEmpty(branchPrincipal))
        {
            script.Append($"git fetch origin {branchPrincipal}; ");
            script.Append($"git branch --track {branchPrincipal} origin/{branchPrincipal}; ");
        }

        if (clone.CriarBranchRemoto)
        {
            if (EhBranchBase(clone.Branch) || string.IsNullOrEmpty(clone.Branch))
                script.Append($" git checkout {(string.IsNullOrEmpty(clone.Branch) ? string.Empty : clone.Branch)} 2>$null; ");

            if (clone.Tipo == "nenhum")
                script.Append($" git checkout -b {clone.Codigo};");
            else
                script.Append($" git checkout -b {clone.Tipo}/{clone.Codigo};");
        }
        else if (!string.IsNullOrEmpty(clone.Branch) && EhBranchBase(clone.Branch))
        {
            script.Append($" git checkout {clone.Branch};");
        }

        return script.ToString();
    }

    private static string MontarComandoClone(bool historicoCompleto, string branch)
    {
        var depth = historicoCompleto ? string.Empty : "--depth 1 ";
        var branchArg = !string.IsNullOrEmpty(branch) && !EhBranchBase(branch)
            ? $"--branch {branch} "
            : string.Empty;

        return $"git clone {depth}{branchArg}".TrimEnd();
    }

    private static bool EhBranchBase(string branch) =>
        branch is "develop" or "dev" or "main" or "master";

    private static async Task<string> ExecutarComandoComRetornoAsync(string comando)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = comando["git ".Length..],
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var processo = Process.Start(psi);
        if (processo is null) return string.Empty;

        var output = await processo.StandardOutput.ReadToEndAsync();
        await processo.WaitForExitAsync();
        return output;
    }
}

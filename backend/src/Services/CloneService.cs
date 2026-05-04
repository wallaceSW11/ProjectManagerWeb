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

        string branchPrincipal = string.Empty;
        if (!EhBranchBase(clone.Branch))
        {
            var existe = await VerificarBranchExisteAsync(gitPrincipal.Url!, clone.Branch);
            if (!existe)
                throw new Exception($"Branch '{clone.Branch}' não encontrada no remote.");

            branchPrincipal = await DetectarBranchPrincipalAsync(gitPrincipal.Url!);
        }

        var scriptPrincipal = MontarScript(diretorioCompleto.ToString(), gitPrincipal.Url!, gitPrincipal.Nome, clone, branchPrincipal);
        ShellExecute.ExecutarComando(scriptPrincipal);

        if (clone.BaixarAgregados)
        {
            gitPrincipal.Agregados?.ForEach(async identificadorAgregado =>
            {
                var agregado = await _repositorioJson.GetByIdAsync(identificadorAgregado);
                if (agregado is null) return;

                var branchPrincipalAgregado = string.Empty;
                var branchNaoBaseAgregado = string.Empty;
                var branchBaseAgregado = string.Empty;

                if (!EhBranchBase(clone.Branch))
                {
                    var existeNoAgregado = await VerificarBranchExisteAsync(agregado.Url!, clone.Branch);
                    if (existeNoAgregado)
                    {
                        branchPrincipalAgregado = await DetectarBranchPrincipalAsync(agregado.Url!);
                        branchNaoBaseAgregado = clone.Branch;
                    }
                    else
                    {
                        branchBaseAgregado = await DetectarBranchPrincipalAsync(agregado.Url!);
                    }
                }

                CloneRequestDTO cloneAgregado;
                if (!string.IsNullOrEmpty(branchNaoBaseAgregado))
                    cloneAgregado = clone with { Branch = branchNaoBaseAgregado };
                else if (!string.IsNullOrEmpty(branchBaseAgregado))
                    cloneAgregado = clone with { Branch = branchBaseAgregado, CriarBranchRemoto = false };
                else
                    cloneAgregado = clone;

                var scriptAgregado = MontarScript(diretorioCompleto.ToString(), agregado.Url!, agregado.Nome, cloneAgregado, branchPrincipalAgregado);
                ShellExecute.ExecutarComando(scriptAgregado);
            });
        }

        return true;
    }

    private static string MontarScript(string diretorioCompleto, string url, string nomeRepo, CloneRequestDTO clone, string branchPrincipal = "")
    {
        // branch não-base: clona a principal, depois faz fetch raso da branch informada
        // branch base: clona direto com --branch, sem baixar outras refs
        var ehNaoBase = !string.IsNullOrEmpty(branchPrincipal);
        var branchParaClone = ehNaoBase ? branchPrincipal : clone.Branch;
        var depth = clone.HistoricoCompleto ? string.Empty : "--depth 1 ";

        StringBuilder script = new();

        script
            .Append($"cd \"{diretorioCompleto}\"; ")
            .Append($"git clone {depth}--branch {branchParaClone} \"{url}\"; ")
            .Append($"cd \"{nomeRepo}\"; ");

        if (ehNaoBase && !string.IsNullOrEmpty(clone.Branch))
        {
            // fetch raso somente da branch não-base criando a ref remota explicitamente
            var depthFetch = clone.HistoricoCompleto ? string.Empty : "--depth 1 ";
            script.Append($"git fetch origin {depthFetch}{clone.Branch}; ");
            script.Append($"git checkout -b {clone.Branch} FETCH_HEAD; ");
        }

        if (clone.CriarBranchRemoto)
        {
            if (clone.Tipo == "nenhum")
                script.Append($"git checkout -b {clone.Codigo};");
            else
                script.Append($"git checkout -b {clone.Tipo}/{clone.Codigo};");
        }

        return script.ToString();
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

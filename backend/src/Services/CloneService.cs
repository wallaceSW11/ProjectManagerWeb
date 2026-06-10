using System.Text;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class CloneService
{
    private readonly RepositorioJsonService _repositorioJson;
    private readonly IGitCommandRunner _gitRunner;

    public CloneService(RepositorioJsonService repositorioJson, IGitCommandRunner gitRunner)
    {
        if (!Directory.Exists(PathHelper.BancoPath))
            Directory.CreateDirectory(PathHelper.BancoPath);

        _repositorioJson = repositorioJson;
        _gitRunner = gitRunner;
    }

    public async Task<(bool Existe, string? Erro)> VerificarBranchExisteAsync(string url, string branch, string? caminhoChaveSSH = null)
    {
        var resultado = await _gitRunner.RunAsync($"git ls-remote --heads \"{url}\" \"{branch}\"", caminhoChaveSSH);

        if (resultado.ExitCode != 0)
        {
            var erro = string.IsNullOrWhiteSpace(resultado.Error)
                ? "Falha ao consultar repositório remoto"
                : resultado.Error.Trim();
            return (false, erro);
        }

        return (!string.IsNullOrWhiteSpace(resultado.Output), null);
    }

    public async Task<string> DetectarBranchPrincipalAsync(string url, string? caminhoChaveSSH = null)
    {
        var resultado = await _gitRunner.RunAsync($"git ls-remote --symref \"{url}\" HEAD", caminhoChaveSSH);

        if (resultado.ExitCode != 0) return string.Empty;

        var linha = resultado.Output
            .Split('\n')
            .FirstOrDefault(l => l.StartsWith("ref: refs/heads/"));

        if (linha is null) return string.Empty;

        return linha.Replace("ref: refs/heads/", "").Split('\t')[0].Trim();
    }

    public async Task<bool> Clonar(CloneRequestDTO clone)
    {
        var nomePasta = $"{clone.Codigo}_{clone.Descricao.Replace(" ", "_")}";
        var diretorioCompleto = Path.Combine(clone.DiretorioRaiz, nomePasta);

        if (!Directory.Exists(diretorioCompleto))
            Directory.CreateDirectory(diretorioCompleto);

        var gitPrincipal = await _repositorioJson.GetByIdAsync(clone.RepositorioId);

        if (gitPrincipal is null)
            return false;

        string branchPrincipal = string.Empty;
        if (!EhBranchBase(clone.Branch))
        {
            var (existe, erro) = await VerificarBranchExisteAsync(gitPrincipal.Url!, clone.Branch, gitPrincipal.CaminhoChaveSSH);
            if (!existe)
            {
                var mensagem = erro is not null
                    ? $"Branch '{clone.Branch}' não encontrada no remote. {erro}"
                    : $"Branch '{clone.Branch}' não encontrada no remote.";
                throw new Exception(mensagem);
            }

            branchPrincipal = await DetectarBranchPrincipalAsync(gitPrincipal.Url!, gitPrincipal.CaminhoChaveSSH);
        }

        var tokenExport = string.IsNullOrEmpty(gitPrincipal.GitHubToken)
            ? string.Empty
            : $"export GH_TOKEN=\"{gitPrincipal.GitHubToken}\"; ";

        var sshExport = string.IsNullOrEmpty(gitPrincipal.CaminhoChaveSSH)
            ? string.Empty
            : $"export GIT_SSH_COMMAND=\"ssh -i {gitPrincipal.CaminhoChaveSSH}\"; ";

        var scriptPrincipal = tokenExport + sshExport + MontarScript(diretorioCompleto, gitPrincipal.Url!, gitPrincipal.Nome, clone, branchPrincipal);
        ShellExecute.ExecutarComando(scriptPrincipal, githubToken: gitPrincipal.GitHubToken);

        if (clone.BaixarAgregados)
        {
            gitPrincipal.Agregados?.ForEach(async identificadorAgregado =>
            {
                var agregado = await _repositorioJson.GetByIdAsync(identificadorAgregado);
                if (agregado is null) return;

                var tokenExportAgregado = string.IsNullOrEmpty(agregado.GitHubToken)
                    ? string.Empty
                    : $"export GH_TOKEN=\"{agregado.GitHubToken}\"; ";

                var sshExportAgregado = string.IsNullOrEmpty(agregado.CaminhoChaveSSH)
                    ? string.Empty
                    : $"export GIT_SSH_COMMAND=\"ssh -i {agregado.CaminhoChaveSSH}\"; ";

                var branchPrincipalAgregado = string.Empty;
                var branchNaoBaseAgregado = string.Empty;
                var branchBaseAgregado = string.Empty;

                if (!EhBranchBase(clone.Branch))
                {
                    var (existeNoAgregado, _) = await VerificarBranchExisteAsync(agregado.Url!, clone.Branch, agregado.CaminhoChaveSSH);
                    if (existeNoAgregado)
                    {
                        branchPrincipalAgregado = await DetectarBranchPrincipalAsync(agregado.Url!, agregado.CaminhoChaveSSH);
                        branchNaoBaseAgregado = clone.Branch;
                    }
                    else
                    {
                        branchBaseAgregado = await DetectarBranchPrincipalAsync(agregado.Url!, agregado.CaminhoChaveSSH);
                    }
                }

                CloneRequestDTO cloneAgregado;
                if (!string.IsNullOrEmpty(branchNaoBaseAgregado))
                    cloneAgregado = clone with { Branch = branchNaoBaseAgregado };
                else if (!string.IsNullOrEmpty(branchBaseAgregado))
                    cloneAgregado = clone with { Branch = branchBaseAgregado, CriarBranchRemoto = false };
                else
                    cloneAgregado = clone;

                var scriptAgregado = tokenExportAgregado + sshExportAgregado + MontarScript(diretorioCompleto, agregado.Url!, agregado.Nome, cloneAgregado, branchPrincipalAgregado);
                ShellExecute.ExecutarComando(scriptAgregado, githubToken: agregado.GitHubToken);
            });
        }

        return true;
    }

    internal static string MontarScript(string diretorioCompleto, string url, string nomeRepo, CloneRequestDTO clone, string branchPrincipal = "")
    {
        // branch não-base: clona a principal, depois faz fetch da branch informada
        // branch base: clona direto com --branch, sem baixar outras refs
        var ehNaoBase = !string.IsNullOrEmpty(branchPrincipal);
        var branchParaClone = ehNaoBase ? branchPrincipal : clone.Branch;
        var filter = clone.HistoricoCompleto ? string.Empty : "--filter=blob:none --single-branch ";

        StringBuilder script = new();

        script
            .Append($"cd \"{diretorioCompleto}\"; ")
            .Append($"git clone {filter}--branch {branchParaClone} \"{url}\"; ")
            .Append($"cd \"{nomeRepo}\"; ");

        if (ehNaoBase && !string.IsNullOrEmpty(clone.Branch))
        {
            script.Append($"git fetch origin {clone.Branch}; ");
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

    internal static bool EhBranchBase(string branch) =>
        branch is "develop" or "dev" or "main" or "master";
}

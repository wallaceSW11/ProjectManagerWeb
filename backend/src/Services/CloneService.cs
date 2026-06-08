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

    public async Task<(bool Existe, string? Erro)> VerificarBranchExisteAsync(string url, string branch, string? caminhoChaveSSH = null)
    {
        var resultado = await ExecutarComandoComRetornoAsync($"git ls-remote --heads \"{url}\" \"{branch}\"", caminhoChaveSSH);

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
        var resultado = await ExecutarComandoComRetornoAsync($"git ls-remote --symref \"{url}\" HEAD", caminhoChaveSSH);

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

        var sshExport = string.IsNullOrEmpty(gitPrincipal.CaminhoChaveSSH)
            ? string.Empty
            : $"export GIT_SSH_COMMAND=\"ssh -i {gitPrincipal.CaminhoChaveSSH}\"; ";

        var scriptPrincipal = sshExport + MontarScript(diretorioCompleto, gitPrincipal.Url!, gitPrincipal.Nome, clone, branchPrincipal);
        ShellExecute.ExecutarComando(scriptPrincipal);

        if (clone.BaixarAgregados)
        {
            gitPrincipal.Agregados?.ForEach(async identificadorAgregado =>
            {
                var agregado = await _repositorioJson.GetByIdAsync(identificadorAgregado);
                if (agregado is null) return;

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

                var scriptAgregado = sshExportAgregado + MontarScript(diretorioCompleto, agregado.Url!, agregado.Nome, cloneAgregado, branchPrincipalAgregado);
                ShellExecute.ExecutarComando(scriptAgregado);
            });
        }

        return true;
    }

    private static string MontarScript(string diretorioCompleto, string url, string nomeRepo, CloneRequestDTO clone, string branchPrincipal = "")
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

    private static bool EhBranchBase(string branch) =>
        branch is "develop" or "dev" or "main" or "master";

    private static async Task<ComandoResultado> ExecutarComandoComRetornoAsync(string comando, string? caminhoChaveSSH = null)
    {
        var useShell = OperatingSystem.IsWindows();

        var psi = new ProcessStartInfo
        {
            FileName = useShell ? "git" : "bash",
            Arguments = useShell ? comando["git ".Length..] : $"-c \"{comando.Replace("\"", "\\\"")}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(caminhoChaveSSH))
            psi.EnvironmentVariables["GIT_SSH_COMMAND"] = $"ssh -i \"{caminhoChaveSSH}\"";

        if (!useShell)
        {
            var sshSock = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK");
            if (!string.IsNullOrEmpty(sshSock))
                psi.EnvironmentVariables["SSH_AUTH_SOCK"] = sshSock;

            var sshPid = Environment.GetEnvironmentVariable("SSH_AGENT_PID");
            if (!string.IsNullOrEmpty(sshPid))
                psi.EnvironmentVariables["SSH_AGENT_PID"] = sshPid;
        }

        using var processo = Process.Start(psi);
        if (processo is null) return new ComandoResultado(string.Empty, "Processo não iniciado", -1);

        var output = await processo.StandardOutput.ReadToEndAsync();
        var error = await processo.StandardError.ReadToEndAsync();
        await processo.WaitForExitAsync();

        return new ComandoResultado(output, FiltrarWarnings(error), processo.ExitCode);
    }

    private static string FiltrarWarnings(string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr)) return string.Empty;

        var linhas = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var semWarnings = linhas.Where(l => !l.StartsWith("Environment variable $"));
        return string.Join('\n', semWarnings);
    }

    private sealed record ComandoResultado(string Output, string Error, int ExitCode);
}

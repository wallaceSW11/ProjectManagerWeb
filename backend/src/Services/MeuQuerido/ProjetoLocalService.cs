using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services.MeuQuerido;

public class ProjetoLocalService(
    PastaService pastaService,
    RepositorioJsonService repositorioJsonService,
    ConfiguracaoService configuracaoService,
    IGitCommandRunner gitRunner)
{
    public async Task<List<ProjetoLocalDTO>> ObterProjetosAsync()
    {
        var raiz = await ObterRaizAsync();

        if (raiz is null) return [];

        var pastas = await pastaService.ObterTodas();
        var projetos = new List<ProjetoLocalDTO>();

        foreach (var pasta in pastas.Where(PodeSerProjeto))
        {
            var projeto = await MontarProjetoAsync(pasta, raiz);

            if (projeto is null) continue;

            projetos.Add(projeto);
        }

        return projetos;
    }

    public async Task<ProjetoLocalDTO?> ObterProjetoAsync(Guid projetoId)
    {
        var raiz = await ObterRaizAsync();

        if (raiz is null) return null;

        var pastas = await pastaService.ObterTodas();
        var pasta = pastas.FirstOrDefault(pasta => pasta.Identificador == projetoId);

        if (pasta is null || !PodeSerProjeto(pasta)) return null;

        return await MontarProjetoAsync(pasta, raiz);
    }

    private async Task<ProjetoLocalDTO?> MontarProjetoAsync(PastaResponseDTO pasta, string raiz)
    {
        var repositorio = await repositorioJsonService.GetByIdAsync(pasta.RepositorioId);

        if (repositorio is null) return null;

        var diretorioTrabalho = Path.Combine(pasta.Diretorio, repositorio.Nome);

        if (!EhRepositorioValido(diretorioTrabalho, raiz)) return null;

        return new ProjetoLocalDTO(
            pasta.Identificador!.Value,
            pasta.Codigo,
            pasta.Descricao,
            repositorio.Nome,
            pasta.Diretorio,
            ValidadorCaminho.Normalizar(diretorioTrabalho),
            await ObterBranchAtualAsync(diretorioTrabalho));
    }

    private async Task<string?> ObterRaizAsync()
    {
        var configuracao = await configuracaoService.ObterConfiguracaoAsync();
        var raiz = configuracao.DiretorioRaizEfetivo;

        return string.IsNullOrWhiteSpace(raiz) ? null : raiz;
    }

    private async Task<string?> ObterBranchAtualAsync(string diretorio)
    {
        var resultado = await gitRunner.RunAsync($"git -C \"{diretorio}\" branch --show-current");

        if (resultado.ExitCode != 0) return null;

        var branch = resultado.Output.Trim();

        return string.IsNullOrWhiteSpace(branch) ? null : branch;
    }

    private static bool PodeSerProjeto(PastaResponseDTO pasta) =>
        pasta.Identificador is not null && pasta.RepositorioId != Guid.Empty;

    private static bool EhRepositorioValido(string diretorio, string raiz) =>
        Directory.Exists(diretorio)
        && TemGit(diretorio)
        && ValidadorCaminho.EstaContidoEm(diretorio, raiz);

    private static bool TemGit(string diretorio) =>
        Directory.Exists(Path.Combine(diretorio, ".git"))
        || File.Exists(Path.Combine(diretorio, ".git"));
}

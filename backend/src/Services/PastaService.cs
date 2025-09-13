using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

public class PastaService(ConfiguracaoService configuracaoService, RepositorioJsonService repositorioJsonService)
{

  public async Task<List<PastaResponseDTO>> ObterTodas()
  {
    var configuracao = await configuracaoService.ObterConfiguracaoAsync();
    var diretorioRaiz = configuracao.DiretorioRaiz;
    var repositorios = await repositorioJsonService.GetAllAsync();

    var pastas = await ObterPastas(configuracao);

    if (pastas.Count == 0)
      return [];

    var pastaResponseList = new List<PastaResponseDTO>();

    foreach (var pasta in pastas)
    {
      var repositorio = repositorios.FirstOrDefault(r => r.Url.Equals(pasta.GitUrl));

      if (repositorio != null)
      {
        var codigo = "";
        string? descricao;
        var tipo = "";

        if (pasta.Diretorio.Contains('_'))
        {
          codigo = pasta.Diretorio.Split("_")[0] ?? "Código não encontrado";
          codigo = codigo.Replace(diretorioRaiz, "").Replace("\\", "");

          descricao = pasta.Diretorio
            .Replace(diretorioRaiz, "")
            .Replace(codigo, "")
            .Replace("_", " ")
            .Replace("\\", "")
            .Replace(repositorio.Nome, "")
            .Trim();

          if (pasta.Branch.Contains('_'))
            tipo = pasta.Branch.Split("/")[0] ?? "nenhum";
        }
        else
        {
          descricao = pasta.Diretorio
            .Replace(diretorioRaiz, "")
            .Replace("\\", "");
        }

        var projetosDisponiveis = new List<ProjetoDisponivelDTO>();

        repositorio.Projetos.ForEach(projeto =>
        {
          var comandos = new List<string>();

          if (!string.IsNullOrWhiteSpace(projeto.Comandos.Instalar))
            comandos.Add("Instalar");

          if (!string.IsNullOrWhiteSpace(projeto.Comandos.Iniciar))
            comandos.Add("Iniciar");

          if (!string.IsNullOrWhiteSpace(projeto.Comandos.Buildar))
            comandos.Add("Buildar");

          if (projeto.Comandos.AbrirNoVSCode)
            comandos.Add("AbrirNoVSCode");

          projetosDisponiveis.Add(new ProjetoDisponivelDTO(projeto.Nome, [.. comandos]));
        });

        var pastaResponse = new PastaResponseDTO
        (
          pasta.Diretorio,
          codigo,
          descricao,
          tipo,
          pasta.Branch,
          pasta.GitUrl,
          repositorio.Id,
          projetosDisponiveis
        );

        pastaResponseList.Add(pastaResponse);

      }
    }

    return pastaResponseList;
  }

  private async Task<List<PastaBaseResponseDTO>> ObterPastas(ConfiguracaoRequestDTO configuracao)
  {
    var diretorioRaiz = configuracao.DiretorioRaiz;
    var pastas = new List<PastaBaseResponseDTO>();

    if (string.IsNullOrWhiteSpace(diretorioRaiz) || !Directory.Exists(diretorioRaiz))
      return pastas;

    // percorre só as pastas diretas (ex: C:\git\PWM1_ProjectManagerWEB)
    foreach (var tarefaDir in Directory.GetDirectories(diretorioRaiz, "*", SearchOption.TopDirectoryOnly))
    {
      var repoDir = EncontrarPastaGit(tarefaDir);
      if (repoDir is null)
        continue;

      var gitDir = Path.Combine(repoDir, ".git");
      string? url = null;
      string? branch = null;

      var configPath = Path.Combine(gitDir, "config");
      var headPath = Path.Combine(gitDir, "HEAD");

      if (File.Exists(configPath))
      {
        var configLines = await File.ReadAllLinesAsync(configPath);
        url = configLines
            .FirstOrDefault(l => l.Trim().StartsWith("url = "))
            ?.Split('=')[1]
            .Trim();
      }

      if (File.Exists(headPath))
      {
        var headLine = await File.ReadAllTextAsync(headPath);
        if (headLine.StartsWith("ref: "))
          branch = headLine.Split('/').Last().Trim();
      }

      if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(branch))
        pastas.Add(new PastaBaseResponseDTO(repoDir, url, branch));
    }

    return pastas;
  }

  /// <summary>
  /// Encontra a primeira subpasta (inclusive a própria) que contenha um .git.
  /// </summary>
  private string? EncontrarPastaGit(string diretorio)
  {
    // se o diretório atual já tem .git, retorna
    if (Directory.Exists(Path.Combine(diretorio, ".git")))
      return diretorio;

    // senão, procura apenas no primeiro nível de subpastas
    foreach (var sub in Directory.GetDirectories(diretorio, "*", SearchOption.TopDirectoryOnly))
    {
      if (Directory.Exists(Path.Combine(sub, ".git")))
        return sub;
    }

    return null;
  }
}
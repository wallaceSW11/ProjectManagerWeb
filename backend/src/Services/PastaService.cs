using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

public class PastaService(ConfiguracaoService configuracaoService, RepositorioJsonService repositorioJsonService)
{

  public async Task<List<PastaResponseDTO>> ObterTodas()
  {
    var configuracao = await configuracaoService.ObterConfiguracaoAsync();
    var diretorioRaiz = configuracao.DiretorioRaiz;

    var pastas = await ObterPastas();

    if (pastas.Count == 0)
      return [];

    var pastaResponseList = new List<PastaResponseDTO>();

    foreach (var pasta in pastas)
    {
      var repositorio = await repositorioJsonService.GetByUrlAsync(pasta.GitUrl);

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

          // foreach (var cmd in projeto.Comandos)
          // {
            if (!string.IsNullOrWhiteSpace(projeto.Comandos.Instalar))
              comandos.Add("Instalar");

            if (!string.IsNullOrWhiteSpace(projeto.Comandos.Iniciar))
              comandos.Add("Iniciar");

            if (!string.IsNullOrWhiteSpace(projeto.Comandos.Buildar))
              comandos.Add("Buildar");

            if (projeto.Comandos.AbrirNoVSCode) // só se quiser considerar
              comandos.Add("AbrirNoVSCode");
          // }

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

  private async Task<List<PastaBaseResponseDTO>> ObterPastas()
  {
    var configuracao = await configuracaoService.ObterConfiguracaoAsync();
    var diretorioRaiz = configuracao.DiretorioRaiz;
    var pastas = new List<PastaBaseResponseDTO>();

    if (string.IsNullOrEmpty(diretorioRaiz) || !Directory.Exists(diretorioRaiz))
      return pastas;

    foreach (var dir in Directory.GetDirectories(diretorioRaiz, "*", SearchOption.AllDirectories))
    {
      var gitDir = Path.Combine(dir, ".git");

      if (Directory.Exists(gitDir))
      {
        string? url = null;
        string? branch = null;
        var configPath = Path.Combine(gitDir, "config");
        var headPath = Path.Combine(gitDir, "HEAD");

        if (File.Exists(configPath))
        {
          var configLines = await File.ReadAllLinesAsync(configPath);
          foreach (var line in configLines)
          {
            if (line.Trim().StartsWith("url = "))
            {
              url = line.Split('=')[1].Trim();
              break;
            }
          }
        }

        if (File.Exists(headPath))
        {
          var headLine = await File.ReadAllTextAsync(headPath);
          if (headLine.StartsWith("ref: "))
          {
            var parts = headLine.Split('/');
            branch = parts.Last().Trim();
          }
        }

        if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(branch))
          pastas.Add(new PastaBaseResponseDTO
          (
            dir,
            url,
            branch
          ));

      }
    }

    return pastas;
  }

  // Additional methods for managing "pasta" entities can be added here
}
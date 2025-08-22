using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

public class PastaService(ConfiguracaoService configuracaoService, RepositorioJsonService repositorioJsonService)
{

  public async Task<List<PastaResponseDTO>> ObterTodas()
  {
    return await ObterPastas();
  }

  private async Task<List<PastaResponseDTO>> ObterPastas()
  {
    var configuracao = await configuracaoService.ObterConfiguracaoAsync();
    var diretorioRaiz = configuracao.DiretorioRaiz;
    var pastas = new List<PastaResponseDTO>();

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
          pastas.Add(new PastaResponseDTO
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
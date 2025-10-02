using System.Net;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

public class PastaService(ConfiguracaoService configuracaoService, RepositorioJsonService repositorioJsonService, PastaJsonService pastaJsonService)
{

  public async Task<List<PastaResponseDTO>> ObterTodas()
  {
    var pastasCadastradas = await pastaJsonService.GetAllAsync();
    var repositorios = await repositorioJsonService.GetAllAsync();

    var configuracao = await configuracaoService.ObterConfiguracaoAsync();
    var diretorioRaiz = configuracao.DiretorioRaiz;

    var pastaResponseList = new List<PastaResponseDTO>();

    var pastasNoDisco = Directory.GetDirectories(diretorioRaiz);

    await LimparPastasInexistentes(pastasCadastradas, pastasNoDisco);

    if (pastasNoDisco.Length == 0)
      return [];

    foreach (var pastaNoDisco in pastasNoDisco)
    {
      var pasta = pastasCadastradas.FirstOrDefault(p => p.Diretorio.Equals(pastaNoDisco, StringComparison.OrdinalIgnoreCase));

      if (pasta == null)
      {
        var indice = new Random().Next(1000, 2000);

        var pastaResponse1 = new PastaResponseDTO
        (
          pastaNoDisco,
          "",
          "",
          "",
          "",
          "",
          new Guid(),
          null,
          [],
          [],
          indice
        );

        pastaResponseList.Add(pastaResponse1);
        continue;
      }

      var repositorio = repositorios.FirstOrDefault(r => r.Identificador == pasta.RepositorioId);

      if (repositorio == null) continue;

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

        projetosDisponiveis.Add(new ProjetoDisponivelDTO(
          projeto.Identificador,
          projeto.Nome,
          repositorio.Nome,
          [.. comandos],
          null,
          projeto.ArquivoCoverage,
          projeto.Subdiretorio
        ));
      });

      repositorio.Agregados?.ForEach(identificadorAgregado =>
      {
        var repositorioAgregado = repositorios.FirstOrDefault(r => r.Identificador == identificadorAgregado);

        if (repositorioAgregado == null) return;

        if (!Directory.Exists(pastaNoDisco + "\\" + repositorioAgregado.Nome) || string.IsNullOrWhiteSpace(repositorioAgregado.Nome))
          return;

        repositorioAgregado.Projetos.ForEach(projeto =>
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

          var nomeProjetoFormatado = $"{projeto.Nome} ({repositorioAgregado.Nome})";

          projetosDisponiveis.Add(new ProjetoDisponivelDTO(
            projeto.Identificador,
            nomeProjetoFormatado,
            repositorioAgregado.Nome,
            [.. comandos],
            repositorioAgregado.Identificador,
            projeto.ArquivoCoverage,
            projeto.Subdiretorio
          ));
        }); 
      });


      var pastaResponse = new PastaResponseDTO
      (
        pasta.Diretorio,
        pasta.Codigo,
        pasta.Descricao,
        pasta.Tipo,
        pasta.Branch,
        repositorio.Url,
        repositorio.Identificador,
        pasta.Identificador,
        projetosDisponiveis,
        repositorio.Menus ?? [],
        pasta.Indice
      );

      pastaResponseList.Add(pastaResponse);
    }

    return [.. pastaResponseList.OrderBy(p => p.Indice)];
  }



  public async Task<PastaCadastroRequestDTO> Cadastrar(PastaCadastroRequestDTO pastaCadastro)
  {
    try
    {
      await pastaJsonService.AddAsync(pastaCadastro);
    } catch (Exception ex)
    {
      throw new Exception($"Erro ao cadastrar a pasta: {ex.Message}");
    }

    return pastaCadastro;
  }

  public async Task<List<PastaCadastroRequestDTO>> ObterTodasAsPastas()
  {
    return await pastaJsonService.GetAllAsync();
  }

  public async Task AtualizarIndices(List<PastaIndiceRequestDTO> indices)
  {
    var pastasExistentes = await pastaJsonService.GetAllAsync();

    foreach (var indice in indices)
    {
      var pastaParaAtualizar = pastasExistentes.FirstOrDefault(p => p.Identificador == indice.Identificador);
      
      if (pastaParaAtualizar != null)
      {
        var pastaAtualizada = pastaParaAtualizar with { Indice = indice.Indice };
        await pastaJsonService.UpdateAsync(pastaParaAtualizar.Diretorio, pastaAtualizada);
      }
    }
  }

  private async Task LimparPastasInexistentes(List<PastaCadastroRequestDTO> pastasCadastradas, string[] pastasNoDisco)
  {
    var pastasParaRemover = new List<PastaCadastroRequestDTO>();

    foreach (var pastaCadastrada in pastasCadastradas)
    {
      var existeNoDisco = pastasNoDisco.Any(pasta => pasta.Equals(pastaCadastrada.Diretorio, StringComparison.OrdinalIgnoreCase));
      
      if (!existeNoDisco)
      {
        pastasParaRemover.Add(pastaCadastrada);
      }
    }

    // Remover pastas que não existem mais no disco
    foreach (var pastaParaRemover in pastasParaRemover)
    {
      await pastaJsonService.DeleteAsync(pastaParaRemover.Diretorio);
    }
  }
}
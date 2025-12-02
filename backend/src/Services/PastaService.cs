using System.Net;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Enuns;

namespace ProjectManagerWeb.src.Services;

public class PastaService(ConfiguracaoService configuracaoService, RepositorioJsonService repositorioJsonService, PastaJsonService pastaJsonService, IDEJsonService ideJsonService)
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

      foreach (var projeto in repositorio.Projetos)
      {
        var comandos = new List<ETipoComando>();

        if (!string.IsNullOrWhiteSpace(projeto.Comandos.Instalar))
          comandos.Add(ETipoComando.INSTALAR);

        if (!string.IsNullOrWhiteSpace(projeto.Comandos.Iniciar))
          comandos.Add(ETipoComando.INICIAR);

        if (!string.IsNullOrWhiteSpace(projeto.Comandos.Buildar))
          comandos.Add(ETipoComando.BUILDAR);

        string? nomeIDE = null;
        if (projeto.Comandos.IDEIdentificador != null)
        {
          comandos.Add(ETipoComando.ABRIR_NA_IDE);
          var ide = await ideJsonService.GetByIdAsync(projeto.Comandos.IDEIdentificador.Value);
          nomeIDE = ide?.Nome;
        }

        projetosDisponiveis.Add(new ProjetoDisponivelDTO(
          projeto.Identificador,
          projeto.Nome,
          repositorio.Nome,
          comandos,
          null,
          projeto.ArquivoCoverage,
          projeto.Subdiretorio,
          projeto.Expandido,
          nomeIDE
        ));
      }

      if (repositorio.Agregados != null)
      {
        foreach (var identificadorAgregado in repositorio.Agregados)
        {
          var repositorioAgregado = repositorios.FirstOrDefault(r => r.Identificador == identificadorAgregado);

          if (repositorioAgregado == null) continue;

          if (!Directory.Exists(pastaNoDisco + "\\" + repositorioAgregado.Nome) || string.IsNullOrWhiteSpace(repositorioAgregado.Nome))
            continue;

          foreach (var projeto in repositorioAgregado.Projetos)
          {
            var comandos = new List<ETipoComando>();

            if (!string.IsNullOrWhiteSpace(projeto.Comandos.Instalar))
              comandos.Add(ETipoComando.INSTALAR);

            if (!string.IsNullOrWhiteSpace(projeto.Comandos.Iniciar))
              comandos.Add(ETipoComando.INICIAR);

            if (!string.IsNullOrWhiteSpace(projeto.Comandos.Buildar))
              comandos.Add(ETipoComando.BUILDAR);

            string? nomeIDE = null;
            if (projeto.Comandos.IDEIdentificador != null)
            {
              comandos.Add(ETipoComando.ABRIR_NA_IDE);
              var ide = await ideJsonService.GetByIdAsync(projeto.Comandos.IDEIdentificador.Value);
              nomeIDE = ide?.Nome;
            }

            var nomeProjetoFormatado = $"{projeto.Nome} ({repositorioAgregado.Nome})";

            projetosDisponiveis.Add(new ProjetoDisponivelDTO(
              projeto.Identificador,
              nomeProjetoFormatado,
              repositorioAgregado.Nome,
              comandos,
              repositorioAgregado.Identificador,
              projeto.ArquivoCoverage,
              projeto.Subdiretorio,
              projeto.Expandido,
              nomeIDE
            ));
          }
        }
      }


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
        repositorio.Cor,
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

  public async Task AtualizarExpandidoProjeto(Guid pastaId, Guid projetoId, bool expandido)
  {
    // Obter a pasta pelo ID
    var pastasExistentes = await pastaJsonService.GetAllAsync();
    var pasta = pastasExistentes.FirstOrDefault(p => p.Identificador == pastaId);
    
    if (pasta == null)
      throw new Exception($"Pasta não encontrada com o identificador {pastaId}");

    // Obter o repositório associado à pasta
    var repositorio = await repositorioJsonService.GetByIdAsync(pasta.RepositorioId);
    
    if (repositorio == null)
      throw new Exception($"Repositório não encontrado com o identificador {pasta.RepositorioId}");

    // Encontrar o projeto no repositório principal
    var projetoEncontrado = repositorio.Projetos.FirstOrDefault(p => p.Identificador == projetoId);
    
    if (projetoEncontrado != null)
    {
      // Atualizar o projeto no repositório principal
      var projetoAtualizado = projetoEncontrado with { Expandido = expandido };
      var projetosAtualizados = repositorio.Projetos.Select(p => 
        p.Identificador == projetoId ? projetoAtualizado : p).ToList();
      
      var repositorioAtualizado = repositorio with { Projetos = projetosAtualizados };
      await repositorioJsonService.UpdateAsync(repositorio.Identificador, repositorioAtualizado);
      return;
    }

    // Se não encontrou no repositório principal, procurar nos agregados
    if (repositorio.Agregados != null)
    {
      foreach (var agregadoId in repositorio.Agregados)
      {
        var repositorioAgregado = await repositorioJsonService.GetByIdAsync(agregadoId);
        
        if (repositorioAgregado == null) continue;

        var projetoAgregado = repositorioAgregado.Projetos.FirstOrDefault(p => p.Identificador == projetoId);
        
        if (projetoAgregado != null)
        {
          // Atualizar o projeto no repositório agregado
          var projetoAtualizado = projetoAgregado with { Expandido = expandido };
          var projetosAtualizados = repositorioAgregado.Projetos.Select(p => 
            p.Identificador == projetoId ? projetoAtualizado : p).ToList();
          
          var repositorioAgregadoAtualizado = repositorioAgregado with { Projetos = projetosAtualizados };
          await repositorioJsonService.UpdateAsync(repositorioAgregado.Identificador, repositorioAgregadoAtualizado);
          return;
        }
      }
    }

    throw new Exception($"Projeto não encontrado com o identificador {projetoId}");
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
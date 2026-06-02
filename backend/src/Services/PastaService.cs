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
    var diretorioRaiz = configuracao.DiretorioRaizEfetivo;

    if (string.IsNullOrWhiteSpace(diretorioRaiz))
        return [];

    var diretoriosOcultos = configuracao.DiretoriosOcultos ?? [];
    var pastasCentralizadoras = configuracao.PastasCentralizadoras ?? [];
    var temPastaCentralizadora = pastasCentralizadoras.Count > 0;

    var pastaResponseList = new List<PastaResponseDTO>();
    var listaPastas = new List<(string Diretorio, string NomeAba)>();

    if (temPastaCentralizadora)
    {
      var nomesPc = pastasCentralizadoras.Select(pc => pc.Nome).ToHashSet(StringComparer.OrdinalIgnoreCase);

      foreach (var pc in pastasCentralizadoras)
      {
        var caminhoPc = Path.Combine(diretorioRaiz, pc.Nome);
        if (!Directory.Exists(caminhoPc)) continue;

        foreach (var subDir in Directory.GetDirectories(caminhoPc))
            if (!diretoriosOcultos.Contains(subDir, StringComparer.OrdinalIgnoreCase))
                listaPastas.Add((subDir, pc.Nome));
      }

      foreach (var dir in Directory.GetDirectories(diretorioRaiz))
      {
        if (diretoriosOcultos.Contains(dir, StringComparer.OrdinalIgnoreCase)) continue;
        var nomeDir = Path.GetFileName(dir);
        if (nomesPc.Contains(nomeDir)) continue;
        listaPastas.Add((dir, "Raiz"));
      }
    }
    else
    {
      foreach (var dir in Directory.GetDirectories(diretorioRaiz))
      {
        if (!diretoriosOcultos.Contains(dir, StringComparer.OrdinalIgnoreCase))
          listaPastas.Add((dir, "Raiz"));
      }
    }

    var pastasNoDisco = listaPastas.Select(lp => lp.Diretorio).ToArray();

    await LimparPastasInexistentes(pastasCadastradas, pastasNoDisco);

    if (pastasNoDisco.Length == 0)
      return [];

    foreach (var (pastaNoDisco, nomeAba) in listaPastas)
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
          null,
          null,
          indice,
          NomeAba: nomeAba
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
          nomeIDE,
          projeto.PerfilTerminal
        ));
      }

      if (repositorio.Agregados != null)
      {
        foreach (var identificadorAgregado in repositorio.Agregados)
        {
          var repositorioAgregado = repositorios.FirstOrDefault(r => r.Identificador == identificadorAgregado);

          if (repositorioAgregado == null) continue;

          if (!Directory.Exists(Path.Combine(pastaNoDisco, repositorioAgregado.Nome)) || string.IsNullOrWhiteSpace(repositorioAgregado.Nome))
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
              nomeIDE,
              projeto.PerfilTerminal
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
        repositorio.IDEIdentificador,
        repositorio.Nome,
        pasta.Indice,
        repositorio.PerfilVSCode,
        repositorio.IDEIdentificador != null
          ? (await ideJsonService.GetByIdAsync(repositorio.IDEIdentificador.Value))?.Nome
          : null,
        repositorio.Subdiretorio,
        repositorio.CliComando,
        repositorio.PerfilTerminal,
        repositorio.AbrirWorkspace,
        pasta.Fixada,
        pasta.OrdemFixada,
        repositorio.CliComandoComplementar,
        repositorio.CliComando != null
          ? configuracao.CLIs?.FirstOrDefault(c => c.Comando.Equals(repositorio.CliComando, StringComparison.OrdinalIgnoreCase))?.Nome
          : null,
        nomeAba,
        repositorio.UrlBaseGestorTarefas
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

  public async Task RemoverPorDiretorio(string diretorio)
  {
    await pastaJsonService.DeleteAsync(diretorio);
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

  public async Task FixarPasta(Guid identificador)
  {
    var pastas = await pastaJsonService.GetAllAsync();
    var pasta = pastas.FirstOrDefault(p => p.Identificador == identificador)
      ?? throw new Exception("Pasta não encontrada");

    var maxOrdem = pastas.Where(p => p.Fixada).Select(p => p.OrdemFixada).DefaultIfEmpty(0).Max();
    var pastaAtualizada = pasta with { Fixada = true, OrdemFixada = maxOrdem + 1 };
    await pastaJsonService.UpdateAsync(pasta.Diretorio, pastaAtualizada);
  }

  public async Task DesfixarPasta(Guid identificador)
  {
    var pastas = await pastaJsonService.GetAllAsync();
    var pasta = pastas.FirstOrDefault(p => p.Identificador == identificador)
      ?? throw new Exception("Pasta não encontrada");

    var pastaAtualizada = pasta with { Fixada = false, OrdemFixada = 0 };
    await pastaJsonService.UpdateAsync(pasta.Diretorio, pastaAtualizada);
  }

  public async Task ReordenarFixadas(List<PastaIndiceRequestDTO> indices)
  {
    var pastas = await pastaJsonService.GetAllAsync();

    foreach (var indice in indices)
    {
      var pasta = pastas.FirstOrDefault(p => p.Identificador == indice.Identificador);
      if (pasta != null && pasta.Fixada)
      {
        var pastaAtualizada = pasta with { OrdemFixada = indice.Indice };
        await pastaJsonService.UpdateAsync(pasta.Diretorio, pastaAtualizada);
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
      await pastaJsonService.DeleteAsync(pastaParaRemover.Diretorio);
  }
}
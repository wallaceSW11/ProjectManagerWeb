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

    if (pastasNoDisco.Length == 0)
      return [];

    foreach (var pastaNoDisco in pastasNoDisco)
    {
      var pasta = pastasCadastradas.FirstOrDefault(p => p.Diretorio.Equals(pastaNoDisco, StringComparison.OrdinalIgnoreCase));

      if (pasta == null)
      {
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
          []
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

        projetosDisponiveis.Add(new ProjetoDisponivelDTO(projeto.Identificador, projeto.Nome, [.. comandos]));
      });

      repositorio.Agregados?.ForEach(identificadorAgregado =>
      {
        var repositorioAgregado = repositorios.FirstOrDefault(r => r.Identificador == identificadorAgregado);

        if (repositorioAgregado == null) return;

        var nomeRepositorioAgregado = RepositorioRequestDTO.ObterNomeRepositorio(repositorioAgregado.Url);

        if (!Directory.Exists(pastaNoDisco + "\\" + nomeRepositorioAgregado) || string.IsNullOrWhiteSpace(nomeRepositorioAgregado))
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
            [.. comandos]
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
        repositorio.Menus ?? []
      );

      pastaResponseList.Add(pastaResponse);
    }

    return pastaResponseList;
  }



  public async Task<PastaCadastroRequestDTO> Cadastrar(PastaCadastroRequestDTO pastaCadastro)
  {
    await pastaJsonService.AddAsync(pastaCadastro);

    return pastaCadastro;
  }

  public async Task<List<PastaCadastroRequestDTO>> ObterTodasAsPastas()
  {
    return await pastaJsonService.GetAllAsync();
  }
}
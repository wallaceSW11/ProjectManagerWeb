using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class ComandoService(RepositorioJsonService repositorioJsonService)
{
  public async Task<bool> ExecutarComando(PastaRequestDTO pasta)
  {
    var repositorio = await repositorioJsonService.GetByIdAsync(pasta.RepositorioId) ?? throw new Exception("Repositório não encontrado");
    var comandos = new List<string>();

    var diretorio = pasta.Diretorio + "\\" + repositorio.Nome + "\\";

    // Projetos originais do repositório
    pasta.Projetos.Where(p => p.IdentificadorRepositorioAgregado is null).ToList().ForEach(projeto =>
    {
      var projetoCadastrado = repositorio.Projetos.FirstOrDefault(p => p.Identificador.Equals(projeto.Identificador)) ?? throw new Exception($"projeto não encontrado com o identificador {projeto.Identificador}");

      projeto.Comandos.ForEach(comando =>
      {
        if (comando.Equals("Iniciar"))
        {
          if (!string.IsNullOrEmpty(projetoCadastrado.Comandos.Instalar) && projetoCadastrado.Comandos.Instalar.Contains("npm i"))
          {
            if (Directory.Exists($"{diretorio}{projetoCadastrado.Subdiretorio}\\node_modules"))
              comandos.Add($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {projetoCadastrado.Comandos.Iniciar}; ");
            else
              comandos.Add($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {projetoCadastrado.Comandos.Instalar}; {projetoCadastrado.Comandos.Iniciar};");
          }
          else
            comandos.Add($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {projetoCadastrado.Comandos.Iniciar}; ");
        }

        if (comando.Equals("Instalar"))
          comandos.Add($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {projetoCadastrado.Comandos.Instalar}; ");

        if (comando.Equals("Buildar"))
          comandos.Add($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {projetoCadastrado.Comandos.Buildar}; ");

        if (comando.Equals("AbrirNoVSCode"))
        {
          var texto = "code . ";

          if (!string.IsNullOrEmpty(projetoCadastrado.PerfilVSCode))
            texto += $"--profile {projetoCadastrado.PerfilVSCode}";

          comandos.Add($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {texto}; Exit;");
        }
      });
    });

    // Processa projetos agregados aguardando adequadamente as operações assíncronas
    foreach (var projeto in pasta.Projetos.Where(p => p.IdentificadorRepositorioAgregado is not null))
    {
      if (projeto.IdentificadorRepositorioAgregado is null) continue;

      var repositorioAgregado = await repositorioJsonService.GetByIdAsync(projeto.IdentificadorRepositorioAgregado.Value) ?? throw new Exception($"Repositório agregado não encontrado com o identificador {projeto.IdentificadorRepositorioAgregado}");

      var projetoAgregadoCadastrado = repositorioAgregado.Projetos.FirstOrDefault(p => p.Identificador.Equals(projeto.Identificador)) ?? throw new Exception($"projeto agregado não encontrado com o identificador {projeto.Identificador}");

      var diretorioAgregado = diretorio.Replace(repositorio.Nome, repositorioAgregado.Nome) + "\\";

      projeto.Comandos.ForEach(comando =>
      {
        if (comando.Equals("Iniciar"))
        {
          if (!string.IsNullOrEmpty(projetoAgregadoCadastrado.Comandos.Instalar) && projetoAgregadoCadastrado.Comandos.Instalar.Contains("npm i"))
          {
            if (Directory.Exists($"{diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}\\node_modules"))
              comandos.Add($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}; {projetoAgregadoCadastrado.Comandos.Iniciar}; ");
            else
              comandos.Add($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}; {projetoAgregadoCadastrado.Comandos.Instalar}; {projetoAgregadoCadastrado.Comandos.Iniciar};");
          }
          else
            comandos.Add($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}; {projetoAgregadoCadastrado.Comandos.Iniciar}; ");
        }

        if (comando.Equals("Instalar"))
          comandos.Add($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Nome}; {projetoAgregadoCadastrado.Comandos.Instalar}; ");

        if (comando.Equals("Buildar"))
          comandos.Add($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Nome}; {projetoAgregadoCadastrado.Comandos.Buildar}; ");

        if (comando.Equals("AbrirNoVSCode"))
        {
          var texto = "code . ";

          if (!string.IsNullOrEmpty(projetoAgregadoCadastrado.PerfilVSCode))
            texto += $"--profile {projetoAgregadoCadastrado.PerfilVSCode}";

          comandos.Add($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}; {texto}; Exit;");
        }
      });
    }

    try
    {
      comandos.ForEach(ShellExecute.ExecutarComando);
    }
    catch
    {
      return false;
    }
    finally
    {
      comandos.Clear();
    }

    return true;
  }

  public bool ExecutarComandoAvulso(string comando)
  {
    try
    {
      ShellExecute.ExecutarComando(comando);
    }
    catch
    {
      return false;
    }

    return true;
  }

  public async Task<bool> ExecutarComandoMenu(MenuRequestDTO menu)
  {
    var repositorio = await repositorioJsonService.GetByIdAsync(menu.RepositorioId) ?? throw new Exception("Repositório não encontrado");
    var menuRepositorio = repositorio.Menus?.FirstOrDefault(m => m.Identificador == menu.ComandoId) ?? throw new Exception("Comando não encontrado");

    var comandos = new List<string>();

    menuRepositorio.Arquivos?.ForEach(a =>
    {
      var nomeArquivo = Path.GetFileName(a.Arquivo);

      if (a.IgnorarGit)
        comandos
          .Add($"Copy-Item \"{a.Arquivo}\" \"{menu.Diretorio}\\{a.Destino}\\{nomeArquivo}\" -Recurse -Force; cd {menu.Diretorio}\\{a.Destino}; git update-index --assume-unchanged {nomeArquivo}; Exit;");
      else
        comandos
          .Add($"Copy-Item \"{a.Arquivo}\" \"{menu.Diretorio}\\{a.Destino}\\{nomeArquivo}\" -Recurse -Force; Exit;");
    });

    try
    {
      comandos.ForEach(ShellExecute.ExecutarComando); 
    }
    catch
    {
      return false;
    }
    finally
    {
      comandos.Clear();
    }

    return true;
  }


}
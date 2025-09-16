using System.ComponentModel;
using System.Text;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class ComandoService(RepositorioJsonService repositorioJsonService)
{
  public async Task<bool> ExecutarComando(PastaRequestDTO pasta)
  {
    var repositorio = await repositorioJsonService.GetByIdAsync(pasta.RepositorioId) ?? throw new Exception("Repositório não encontrado");
    var comandos = new List<string>();

    var diretorio = pasta.Diretorio + "\\";

    pasta.Projetos.ForEach(projeto =>
    {
      var projetoCadastrado = repositorio.Projetos.FirstOrDefault(p => p.Nome.Equals(projeto.Nome)) ?? throw new Exception("projeto não encontrado");

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
          comandos.Add($"cd {diretorio}{projetoCadastrado.Nome}; {projetoCadastrado.Comandos.Instalar}; ");

        if (comando.Equals("Buildar"))
          comandos.Add($"cd {diretorio}{projetoCadastrado.Nome}; {projetoCadastrado.Comandos.Buildar}; ");

        if (comando.Equals("AbrirNoVSCode"))
        {
          var texto = "code . ";

          if (!string.IsNullOrEmpty(projetoCadastrado.PerfilVSCode))
            texto += $"--profile {projetoCadastrado.PerfilVSCode}";

          comandos.Add($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {texto}; exit;");
        }
      });
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
    var menuRepositorio = repositorio.Menus?.FirstOrDefault(m => m.Id == menu.ComandoId) ?? throw new Exception("Comando não encontrado");
    var nomeRepositorio = RepositorioRequestDTO.ObterNomeRepositorio(repositorio.Url);

    var comandos = new List<string>();

    menuRepositorio.Arquivos?.ForEach(a =>
    {
      var nomeArquivo = Path.GetFileName(a.Arquivo);

      comandos
        .Add($"Copy-Item \"{a.Arquivo}\" \"{menu.Diretorio}\\{a.Destino}\\{nomeArquivo}\" -Recurse -Force; Exit;");
 
      if (a.IgnorarGit)
        comandos.Add($"cd {menu.Diretorio}\\{a.Destino}; git update-index --no-assume-unchanged {nomeArquivo}; Exit");
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
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class ComandoService(RepositorioJsonService repositorioJsonService)
{
  public async Task<bool> ExecutarComando(PastaRequestDTO pasta)
  {
    var repositorio = await repositorioJsonService.GetByIdAsync(pasta.GitId) ?? throw new Exception("Repositório não encontrado");
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
  
}
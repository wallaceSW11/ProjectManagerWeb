using System.Text;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class ComandoService(ConfiguracaoService configuracaoService, RepositorioJsonService repositorioJsonService)
{

  public async Task<bool> ExecutarComando(PastaRequestDTO pasta)
  {
    var repositorio = await repositorioJsonService.GetByIdAsync(pasta.GitId) ?? throw new Exception("Repositório não encontrado");
    var comandos = new List<string>();

    var diretorio = pasta.Diretorio + "\\" + repositorio.Nome + "\\";

    pasta.Projetos.ForEach(projeto =>
    {
      var projetoCadastrado = repositorio.Projetos.FirstOrDefault(p => p.Nome.Equals(projeto.Nome)) ?? throw new Exception("projeto não encontrado");

      projeto.Comandos.ForEach(comando =>
      {
        if (comando.Equals("Iniciar"))
        {
          if (Directory.Exists($"{diretorio}{projetoCadastrado.Nome}\\node_modules"))
            comandos.Add($"cd {diretorio}{projetoCadastrado.Nome}; {projetoCadastrado.Comandos.Iniciar}; ");
          else
            comandos.Add($"cd {diretorio}{projetoCadastrado.Nome}; {projetoCadastrado.Comandos.Instalar}; {projetoCadastrado.Comandos.Iniciar};");
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

          comandos.Add($"cd {diretorio}{projetoCadastrado.Nome}; {texto}; exit;");
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

    return true;
  }
}
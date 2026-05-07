using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;
using ProjectManagerWeb.src.Enuns;

namespace ProjectManagerWeb.src.Services;

public class ComandoService(RepositorioJsonService repositorioJsonService, IDEJsonService ideJsonService)
{
  public async Task<bool> ExecutarComando(PastaRequestDTO pasta)
  {
    var repositorio = await repositorioJsonService.GetByIdAsync(pasta.RepositorioId) ?? throw new Exception("Repositório não encontrado");
    var comandos = new List<(string Comando, string? Perfil)>();

    var diretorio = pasta.Diretorio + "\\" + repositorio.Nome + "\\";

    foreach (var projeto in pasta.Projetos.Where(p => p.IdentificadorRepositorioAgregado is null))
    {
      var projetoCadastrado = repositorio.Projetos.FirstOrDefault(p => p.Identificador.Equals(projeto.Identificador)) ?? throw new Exception($"projeto não encontrado com o identificador {projeto.Identificador}");

      foreach (var comando in projeto.Comandos)
      {
        if (comando == ETipoComando.INICIAR)
        {
          if (!string.IsNullOrEmpty(projetoCadastrado.Comandos.Instalar) && projetoCadastrado.Comandos.Instalar.Contains("npm i"))
          {
            if (Directory.Exists($"{diretorio}{projetoCadastrado.Subdiretorio}\\node_modules"))
              comandos.Add(($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {projetoCadastrado.Comandos.Iniciar}; ", projetoCadastrado.PerfilTerminal));
            else
              comandos.Add(($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {projetoCadastrado.Comandos.Instalar}; {projetoCadastrado.Comandos.Iniciar};", projetoCadastrado.PerfilTerminal));
          }
          else
            comandos.Add(($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {projetoCadastrado.Comandos.Iniciar}; ", projetoCadastrado.PerfilTerminal));
        }

        if (comando == ETipoComando.INSTALAR)
          comandos.Add(($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {projetoCadastrado.Comandos.Instalar}; ", projetoCadastrado.PerfilTerminal));

        if (comando == ETipoComando.BUILDAR)
          comandos.Add(($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {projetoCadastrado.Comandos.Buildar}; ", projetoCadastrado.PerfilTerminal));

        if (comando == ETipoComando.ABRIR_NA_IDE)
        {
          if (projetoCadastrado.Comandos.IDEIdentificador != null)
          {
            var ide = await ideJsonService.GetByIdAsync(projetoCadastrado.Comandos.IDEIdentificador.Value);
            if (ide != null)
            {
              var texto = ide.ComandoParaExecutar + " ";

              if (ide.AceitaPerfilPersonalizado && !string.IsNullOrEmpty(projetoCadastrado.PerfilVSCode))
                texto += $"--profile \"{projetoCadastrado.PerfilVSCode}\"";

              comandos.Add(($"cd {diretorio}{projetoCadastrado.Subdiretorio}; {texto}; Exit;", null));
            }
          }
        }
      }
    }

    foreach (var projeto in pasta.Projetos.Where(p => p.IdentificadorRepositorioAgregado is not null))
    {
      if (projeto.IdentificadorRepositorioAgregado is null) continue;

      var repositorioAgregado = await repositorioJsonService.GetByIdAsync(projeto.IdentificadorRepositorioAgregado.Value) ?? throw new Exception($"Repositório agregado não encontrado com o identificador {projeto.IdentificadorRepositorioAgregado}");

      var projetoAgregadoCadastrado = repositorioAgregado.Projetos.FirstOrDefault(p => p.Identificador.Equals(projeto.Identificador)) ?? throw new Exception($"projeto agregado não encontrado com o identificador {projeto.Identificador}");

      var diretorioAgregado = diretorio.Replace(repositorio.Nome, repositorioAgregado.Nome) + "\\";

      foreach (var comando in projeto.Comandos)
      {
        if (comando == ETipoComando.INICIAR)
        {
          if (!string.IsNullOrEmpty(projetoAgregadoCadastrado.Comandos.Instalar) && projetoAgregadoCadastrado.Comandos.Instalar.Contains("npm i"))
          {
            if (Directory.Exists($"{diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}\\node_modules"))
              comandos.Add(($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}; {projetoAgregadoCadastrado.Comandos.Iniciar}; ", projetoAgregadoCadastrado.PerfilTerminal));
            else
              comandos.Add(($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}; {projetoAgregadoCadastrado.Comandos.Instalar}; {projetoAgregadoCadastrado.Comandos.Iniciar};", projetoAgregadoCadastrado.PerfilTerminal));
          }
          else
            comandos.Add(($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}; {projetoAgregadoCadastrado.Comandos.Iniciar}; ", projetoAgregadoCadastrado.PerfilTerminal));
        }

        if (comando == ETipoComando.INSTALAR)
          comandos.Add(($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}; {projetoAgregadoCadastrado.Comandos.Instalar}; ", projetoAgregadoCadastrado.PerfilTerminal));

        if (comando == ETipoComando.BUILDAR)
          comandos.Add(($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}; {projetoAgregadoCadastrado.Comandos.Buildar}; ", projetoAgregadoCadastrado.PerfilTerminal));

        if (comando == ETipoComando.ABRIR_NA_IDE)
        {
          if (projetoAgregadoCadastrado.Comandos.IDEIdentificador != null)
          {
            var ide = await ideJsonService.GetByIdAsync(projetoAgregadoCadastrado.Comandos.IDEIdentificador.Value);
            if (ide != null)
            {
              var texto = ide.ComandoParaExecutar + " ";

              if (ide.AceitaPerfilPersonalizado && !string.IsNullOrEmpty(projetoAgregadoCadastrado.PerfilVSCode))
                texto += $"--profile \"{projetoAgregadoCadastrado.PerfilVSCode}\"";

              comandos.Add(($"cd {diretorioAgregado}{projetoAgregadoCadastrado.Subdiretorio}; {texto}; Exit;", null));
            }
          }
        }
      }
    }

    try
    {
      comandos.ForEach(c => ShellExecute.ExecutarComando(c.Comando, c.Perfil));
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

  public bool ExecutarComandoAvulso(string comando, string? perfilTerminal = null)
  {
    try
    {
      ShellExecute.ExecutarComando(comando, perfilTerminal);
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
      var diretorioDestino = Path.Combine(menu.Diretorio, a.Destino);

      if (!Directory.Exists(diretorioDestino))
        Directory.CreateDirectory(diretorioDestino);

      if (a.IgnorarGit)
      {
        var caminhoArquivoDestino = Path.Combine(diretorioDestino, nomeArquivo);
        var sucesso = CopiarArquivoComRetry(a.Arquivo, caminhoArquivoDestino);
        
        if (sucesso)
          IgnorarArquivoNoGitComRetry(diretorioDestino, nomeArquivo);
      }
      else
      {
        comandos.Add($"Copy-Item \"{a.Arquivo}\" \"{diretorioDestino}\\{nomeArquivo}\" -Recurse -Force; Exit;");
      }
    });

    menuRepositorio.Pastas?.ForEach(p =>
    {
      var caminhoDestinoCompleto = Path.Combine(menu.Diretorio, p.Destino);

      if (!Directory.Exists(caminhoDestinoCompleto))
        Directory.CreateDirectory(caminhoDestinoCompleto);

      comandos.Add($"Copy-Item \"{p.Origem}\" \"{caminhoDestinoCompleto}\" -Recurse -Force; Exit;");
    });

    try
    {
      comandos.ForEach(c => ShellExecute.ExecutarComando(c)); 
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

  private bool CopiarArquivoComRetry(string origem, string destino, int maxTentativas = 3)
  {
    for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
    {
      try
      {
        var comando = $"Copy-Item \"{origem}\" \"{destino}\" -Recurse -Force; Exit;";
        ShellExecute.ExecutarComando(comando);

        Thread.Sleep(200);

        if (File.Exists(destino))
          return true;
        
        if (tentativa < maxTentativas)
          Thread.Sleep(200 * tentativa);
      }
      catch
      {
        if (tentativa < maxTentativas)
          Thread.Sleep(200 * tentativa);
      }
    }

    return false;
  }

  private void IgnorarArquivoNoGitComRetry(string diretorio, string nomeArquivo, int maxTentativas = 3)
  {
    for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
    {
      try
      {
        var comando = $"cd {diretorio}; git update-index --assume-unchanged {nomeArquivo}; Exit;";
        ShellExecute.ExecutarComando(comando);
        return;
      }
      catch
      {
        if (tentativa < maxTentativas)
          Thread.Sleep(200 * tentativa);
      }
    }
  }

  public async Task<bool> AbrirPastaIDE(AbrirPastaIDERequestDTO request)
  {
    var ide = await ideJsonService.GetByIdAsync(request.IDEIdentificador) ?? throw new Exception("IDE não encontrada");

    var alvo = ObterAlvoIDE(request.Diretorio);

    var texto = $"{ide.ComandoParaExecutar} {alvo}";

    if (ide.AceitaPerfilPersonalizado && !string.IsNullOrEmpty(request.PerfilVSCode))
      texto = $"{ide.ComandoParaExecutar} --profile \"{request.PerfilVSCode}\" {alvo}";

    var comando = $"cd {request.Diretorio}; {texto}; Exit;";

    try
    {
      ShellExecute.ExecutarComandoSemInterface(comando);
    }
    catch
    {
      return false;
    }

    return true;
  }

  private static string ObterAlvoIDE(string diretorio)
  {
    if (Directory.Exists(diretorio))
    {
      var workspace = Directory.GetFiles(diretorio, "*.code-workspace").FirstOrDefault();
      if (workspace != null)
        return $"\"{workspace}\"";
    }

    return ".";
  }
}
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

    var diretorio = Path.Combine(pasta.Diretorio, repositorio.Nome);

    foreach (var projeto in pasta.Projetos.Where(p => p.IdentificadorRepositorioAgregado is null))
    {
      var projetoCadastrado = repositorio.Projetos.FirstOrDefault(p => p.Identificador.Equals(projeto.Identificador)) ?? throw new Exception($"projeto não encontrado com o identificador {projeto.Identificador}");

      var pathProjeto = Path.Combine(diretorio, projetoCadastrado.Subdiretorio ?? "");

      foreach (var comando in projeto.Comandos)
      {
        if (comando == ETipoComando.INICIAR)
        {
          if (!string.IsNullOrEmpty(projetoCadastrado.Comandos.Instalar) && projetoCadastrado.Comandos.Instalar.Contains("npm i"))
          {
            if (Directory.Exists(Path.Combine(pathProjeto, "node_modules")))
              comandos.Add(($"cd \"{pathProjeto}\"; {projetoCadastrado.Comandos.Iniciar}; ", projetoCadastrado.PerfilTerminal));
            else
              comandos.Add(($"cd \"{pathProjeto}\"; {projetoCadastrado.Comandos.Instalar}; {projetoCadastrado.Comandos.Iniciar};", projetoCadastrado.PerfilTerminal));
          }
          else
            comandos.Add(($"cd \"{pathProjeto}\"; {projetoCadastrado.Comandos.Iniciar}; ", projetoCadastrado.PerfilTerminal));
        }

        if (comando == ETipoComando.INSTALAR)
          comandos.Add(($"cd \"{pathProjeto}\"; {projetoCadastrado.Comandos.Instalar}; ", projetoCadastrado.PerfilTerminal));

        if (comando == ETipoComando.BUILDAR)
          comandos.Add(($"cd \"{pathProjeto}\"; {projetoCadastrado.Comandos.Buildar}; ", projetoCadastrado.PerfilTerminal));

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

              comandos.Add(($"cd \"{pathProjeto}\"; {texto}; Exit;", null));
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

      var diretorioAgregado = Path.Combine(pasta.Diretorio, repositorioAgregado.Nome);

      foreach (var comando in projeto.Comandos)
      {
        var pathProjetoAgregado = Path.Combine(diretorioAgregado, projetoAgregadoCadastrado.Subdiretorio ?? "");

        if (comando == ETipoComando.INICIAR)
        {
          if (!string.IsNullOrEmpty(projetoAgregadoCadastrado.Comandos.Instalar) && projetoAgregadoCadastrado.Comandos.Instalar.Contains("npm i"))
          {
            if (Directory.Exists(Path.Combine(pathProjetoAgregado, "node_modules")))
              comandos.Add(($"cd \"{pathProjetoAgregado}\"; {projetoAgregadoCadastrado.Comandos.Iniciar}; ", projetoAgregadoCadastrado.PerfilTerminal));
            else
              comandos.Add(($"cd \"{pathProjetoAgregado}\"; {projetoAgregadoCadastrado.Comandos.Instalar}; {projetoAgregadoCadastrado.Comandos.Iniciar};", projetoAgregadoCadastrado.PerfilTerminal));
          }
          else
            comandos.Add(($"cd \"{pathProjetoAgregado}\"; {projetoAgregadoCadastrado.Comandos.Iniciar}; ", projetoAgregadoCadastrado.PerfilTerminal));
        }

        if (comando == ETipoComando.INSTALAR)
          comandos.Add(($"cd \"{pathProjetoAgregado}\"; {projetoAgregadoCadastrado.Comandos.Instalar}; ", projetoAgregadoCadastrado.PerfilTerminal));

        if (comando == ETipoComando.BUILDAR)
          comandos.Add(($"cd \"{pathProjetoAgregado}\"; {projetoAgregadoCadastrado.Comandos.Buildar}; ", projetoAgregadoCadastrado.PerfilTerminal));

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

              comandos.Add(($"cd \"{pathProjetoAgregado}\"; {texto}; Exit;", null));
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

    menuRepositorio.Arquivos?.ForEach(a =>
    {
      var nomeArquivo = Path.GetFileName(a.Arquivo);
      var diretorioDestino = Path.Combine(menu.Diretorio, a.Destino);
      var caminhoArquivoDestino = Path.Combine(diretorioDestino, nomeArquivo);

      try
      {
        if (!Directory.Exists(diretorioDestino))
          Directory.CreateDirectory(diretorioDestino);

        File.Copy(a.Arquivo, caminhoArquivoDestino, overwrite: true);

        _ = ShellExecute.LogComandoAsync($"File.Copy \"{a.Arquivo}\" -> \"{caminhoArquivoDestino}\"", "OK");

        if (a.IgnorarGit)
          IgnorarArquivoNoGit(diretorioDestino, nomeArquivo);
      }
      catch (Exception ex)
      {
        _ = ShellExecute.LogComandoAsync($"File.Copy \"{a.Arquivo}\" -> \"{caminhoArquivoDestino}\"", $"ERRO: {ex.Message}");
      }
    });

    menuRepositorio.Pastas?.ForEach(p =>
    {
      try
      {
        var nomePastaOrigem = Path.GetFileName(p.Origem.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var caminhoDestinoCompleto = string.IsNullOrEmpty(p.Destino)
          ? Path.Combine(menu.Diretorio, nomePastaOrigem)
          : Path.Combine(menu.Diretorio, p.Destino);

        CopiarDiretorioRecursivo(p.Origem, caminhoDestinoCompleto);
        _ = ShellExecute.LogComandoAsync($"CopyDir \"{p.Origem}\" -> \"{caminhoDestinoCompleto}\"", "OK");
      }
      catch (Exception ex)
      {
        _ = ShellExecute.LogComandoAsync($"CopyDir \"{p.Origem}\" -> ...", $"ERRO: {ex.Message}");
      }
    });

    return true;
  }

  private static void CopiarDiretorioRecursivo(string origem, string destino)
  {
    Directory.CreateDirectory(destino);

    foreach (var arquivo in Directory.GetFiles(origem))
      File.Copy(arquivo, Path.Combine(destino, Path.GetFileName(arquivo)), true);

    foreach (var subDir in Directory.GetDirectories(origem))
      CopiarDiretorioRecursivo(subDir, Path.Combine(destino, Path.GetFileName(subDir)));
  }

  private static void IgnorarArquivoNoGit(string diretorio, string nomeArquivo)
  {
    try
    {
      var comando = $"cd \"{diretorio}\"; git update-index --assume-unchanged {nomeArquivo}; Exit;";
      ShellExecute.ExecutarComando(comando);
    }
    catch
    {
      // Ignora erro de git — não é crítico
    }
  }

  public async Task<bool> AbrirPastaIDE(AbrirPastaIDERequestDTO request)
  {
    var ide = await ideJsonService.GetByIdAsync(request.IDEIdentificador) ?? throw new Exception("IDE não encontrada");

    var alvo = ObterAlvoIDE(request.Diretorio, request.AbrirWorkspace);
    var comandoBase = alvo == "." ? ide.ComandoParaExecutar : ide.ComandoParaExecutar.TrimEnd(' ', '.');

    var texto = $"{comandoBase} {alvo}";

    if (ide.AceitaPerfilPersonalizado && !string.IsNullOrEmpty(request.PerfilVSCode))
      texto = $"{comandoBase} --profile \"{request.PerfilVSCode}\" {alvo}";

    var comando = $"cd \"{request.Diretorio}\"; {texto}; Exit;";

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

  private static string ObterAlvoIDE(string diretorio, bool abrirWorkspace)
  {
    if (abrirWorkspace && Directory.Exists(diretorio))
    {
      var workspace = Directory.GetFiles(diretorio, "*.code-workspace").FirstOrDefault();
      if (workspace != null)
        return $"\"{workspace}\"";
    }

    return ".";
  }
}

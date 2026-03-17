using System.Text;
using System.Text.Json;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class CloneService
{
    private const string FilePath = "Banco\\pastas.json";
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly RepositorioJsonService _repositorioJson;

    public CloneService(RepositorioJsonService repositorioJson)
    {
        var directoryName = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        _repositorioJson = repositorioJson;
    }

    public async Task<bool> Clonar(CloneRequestDTO clone, bool aguardar = false)
    {
        StringBuilder diretorioCompleto = new();
        StringBuilder comando = new();

        diretorioCompleto
            .Append(clone.DiretorioRaiz)
            .Append(clone.Codigo)
            .Append('_')
            .Append(clone.Descricao.Replace(" ", "_"));

        if (!Directory.Exists(diretorioCompleto.ToString()))
            Directory.CreateDirectory(diretorioCompleto.ToString());

        var gitPrincipal = await _repositorioJson.GetByIdAsync(clone.RepositorioId);

        if (gitPrincipal is null)
            return false;

        var dirRepo = Path.Combine(diretorioCompleto.ToString(), gitPrincipal.Nome);
        var jaClonado = Directory.Exists(Path.Combine(dirRepo, ".git"));

        // Se existe a pasta mas sem .git (clone incompleto), remove para reclone limpo
        if (Directory.Exists(dirRepo) && !jaClonado)
            Directory.Delete(dirRepo, true);

        comando.Append($"cd \"{diretorioCompleto}\"; ");

        if (jaClonado)
            comando.Append($"cd \"{gitPrincipal.Nome}\"; ");
        else
            comando.Append($"git clone {gitPrincipal.Url}; cd \"{gitPrincipal.Nome}\"; ");

        if (clone.CriarBranchRemoto)
        {
            comando.Append($"git checkout {clone.Branch} 2>$null; ");
            if (clone.Tipo == "nenhum")
                comando.Append($"git checkout -b {clone.Codigo};");
            else
                comando.Append($"git checkout -b {clone.Tipo}/{clone.Codigo};");
        }
        else
        {
            comando.Append($"git checkout {clone.Branch};");
        }

        if (aguardar)
            // JARVAS: aguarda terminar para poder sequenciar menus depois
            await ShellExecute.ExecutarComandoAguardarAsync(comando.ToString());
        else
            // Fluxo manual: fire-and-forget, abre pwsh visível e retorna imediatamente
            ShellExecute.ExecutarComando(comando.ToString());

        if (clone.BaixarAgregados)
        {
            var agregadosTasks = (gitPrincipal.Agregados ?? []).Select(async identificadorAgregado =>
            {
                var agregado = await _repositorioJson.GetByIdAsync(identificadorAgregado);
                if (agregado is null) return;

                var dirAgregado = Path.Combine(diretorioCompleto.ToString(), agregado.Nome);
                var agregadoJaClonado = Directory.Exists(Path.Combine(dirAgregado, ".git"));

                if (Directory.Exists(dirAgregado) && !agregadoJaClonado)
                    Directory.Delete(dirAgregado, true);

                var cmdAgregado = new StringBuilder();
                cmdAgregado.Append($"cd \"{diretorioCompleto}\"; ");

                if (agregadoJaClonado)
                    cmdAgregado.Append($"cd \"{agregado.Nome}\"; ");
                else
                    cmdAgregado.Append($"git clone {agregado.Url}; cd \"{agregado.Nome}\"; ");

                if (clone.CriarBranchRemoto)
                {
                    cmdAgregado.Append($"git checkout {clone.Branch} 2>$null; ");
                    if (clone.Tipo == "nenhum")
                        cmdAgregado.Append($"git checkout -b {clone.Codigo};");
                    else
                        cmdAgregado.Append($"git checkout -b {clone.Tipo}/{clone.Codigo};");
                }
                else
                {
                    cmdAgregado.Append($"git checkout {clone.Branch};");
                }

                if (aguardar)
                    await ShellExecute.ExecutarComandoAguardarAsync(cmdAgregado.ToString());
                else
                    ShellExecute.ExecutarComando(cmdAgregado.ToString());
            });

            await Task.WhenAll(agregadosTasks);
        }

        return true;
    }
}

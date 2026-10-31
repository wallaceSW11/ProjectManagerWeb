using System.Diagnostics;
using System.Text.Json;

namespace ProjectManagerWeb.src.Services.MeuQuerido;

public class OpenCodeDescoberta
{
    private const string NomeBinario = "opencode";
    private static readonly TimeSpan TimeoutProcesso = TimeSpan.FromSeconds(5);

    public static string ObterCaminhoFallbackBinario() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".opencode",
            "bin",
            NomeBinario);

    public async Task<ResultadoDescoberta> DescobrirAsync(CancellationToken ct = default)
    {
        var binario = LocalizarBinario();

        if (binario is null)
            return ResultadoDescoberta.Falha(
                $"Binário '{NomeBinario}' não encontrado no PATH nem em {ObterCaminhoFallbackBinario()}");

        var (diretorioEstado, erroProcesso) = await ObterDiretorioEstadoAsync(binario, ct);

        if (diretorioEstado is null)
            return ResultadoDescoberta.Falha(erroProcesso!);

        var registro = ObterRegistro(diretorioEstado);

        if (registro is null)
            return ResultadoDescoberta.Falha(
                $"Registro do serviço OpenCode não encontrado em {diretorioEstado}");

        return ResultadoDescoberta.ComSucesso(registro.Url, registro.Senha);
    }

    public string? LocalizarBinario()
    {
        var caminhoPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        var encontrado = caminhoPath
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(diretorio => Path.Combine(diretorio.Trim(), NomeBinario))
            .FirstOrDefault(File.Exists);

        if (encontrado is not null) return encontrado;

        var fallback = ObterCaminhoFallbackBinario();
        return File.Exists(fallback) ? fallback : null;
    }

    public async Task<(string? Diretorio, string? Erro)> ObterDiretorioEstadoAsync(
        string binario,
        CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeoutProcesso);

        var inicio = new ProcessStartInfo
        {
            FileName = binario,
            Arguments = "debug paths state",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var processo = Process.Start(inicio);

            if (processo is null)
                return (null, $"Não foi possível iniciar '{binario} debug paths state'");

            var saida = await processo.StandardOutput.ReadToEndAsync(cts.Token);
            var erro = await processo.StandardError.ReadToEndAsync(cts.Token);

            await processo.WaitForExitAsync(cts.Token);

            var diretorio = saida.Trim();

            if (processo.ExitCode != 0 || string.IsNullOrWhiteSpace(diretorio))
                return (null, $"'{NomeBinario} debug paths state' falhou: {Mensagem(erro)}");

            return Directory.Exists(diretorio)
                ? (diretorio, null)
                : (null, $"Diretório de estado do OpenCode não existe: {diretorio}");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return (null, $"'{NomeBinario} debug paths state' não respondeu em {TimeoutProcesso.TotalSeconds}s");
        }
        catch (Exception ex)
        {
            return (null, $"Falha ao executar '{binario}': {ex.Message}");
        }
    }

    public RegistroServico? ObterRegistro(string diretorioEstado)
    {
        try
        {
            if (!Directory.Exists(diretorioEstado)) return null;

            var arquivos = Directory.GetFiles(diretorioEstado, "service*.json");
            var caminho = arquivos.FirstOrDefault(arquivo => Path.GetFileName(arquivo) == "service.json")
                ?? arquivos.FirstOrDefault();

            if (caminho is null) return null;

            using var documento = JsonDocument.Parse(File.ReadAllText(caminho));
            var raiz = documento.RootElement;

            var url = LerTexto(raiz, "url");
            var senha = LerTexto(raiz, "password");

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(senha))
                return null;

            return new RegistroServico(url, senha);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? LerTexto(JsonElement raiz, string propriedade) =>
        raiz.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()
            : null;

    private static string Mensagem(string erro) =>
        string.IsNullOrWhiteSpace(erro) ? "sem detalhe" : erro.Trim();
}

public sealed record RegistroServico(string Url, string Senha);

public sealed record ResultadoDescoberta(string? Url, string? Senha, string? Motivo)
{
    public bool Sucesso => Motivo is null;

    public static ResultadoDescoberta ComSucesso(string url, string senha) => new(url, senha, null);

    public static ResultadoDescoberta Falha(string motivo) => new(null, null, motivo);
}

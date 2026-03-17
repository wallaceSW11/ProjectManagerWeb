using System.Text.Json;
using System.Text.Json.Nodes;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Enuns;

namespace ProjectManagerWeb.src.Services;

/// <summary>
/// Recebe a decisão do LLM (tool call ou texto) e executa a ação correspondente
/// usando os services já existentes. Centraliza todo o tratamento de erro.
/// </summary>
public class JarvasOrquestrador(
    CloneService cloneService,
    ComandoService comandoService,
    ConfiguracaoService configuracaoService,
    PastaService pastaService,
    RepositorioJsonService repositorioJsonService)
{
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<(string resposta, string? acaoExecutada)> ExecutarAsync(LLMRespostaDTO llmResposta)
    {
        // LLM respondeu com texto simples — sem ação
        if (!llmResposta.EhToolCall || string.IsNullOrEmpty(llmResposta.ToolNome))
            return (llmResposta.TextoResposta ?? "Não entendi. Pode reformular?", null);

        try
        {
            return llmResposta.ToolNome switch
            {
                "clonar_repositorio"      => await ExecutarClone(llmResposta.ToolArgumentosJson),
                "executar_comandos_pasta" => await ExecutarComandosPasta(llmResposta.ToolArgumentosJson),
                "abrir_na_ide"            => await ExecutarAbrirIDE(llmResposta.ToolArgumentosJson),
                "executar_menu"           => await ExecutarMenu(llmResposta.ToolArgumentosJson),
                "executar_comando_avulso" => ExecutarComandoAvulso(llmResposta.ToolArgumentosJson),
                "resposta_texto"          => ExtrairRespostaTexto(llmResposta.ToolArgumentosJson),
                _                         => ("Recebi uma ação que não sei executar. Pode reformular?", null)
            };
        }
        catch (Exception ex)
        {
            return ($"Entendi o que você quis dizer, mas algo deu errado na execução: {ex.Message}", llmResposta.ToolNome);
        }
    }

    /// <summary>
    /// Executa uma lista de ações detectadas pelo pré-parser (sem LLM).
    /// </summary>
    public async Task<(string resposta, List<string> acoesExecutadas)> ExecutarFilaAsync(List<AcaoDetectada> acoes)
    {
        var executadas = new List<string>();
        var resultados = new List<string>();

        foreach (var acao in acoes)
        {
            try
            {
                var argsJson = System.Text.Json.JsonSerializer.Serialize(acao.Argumentos);
                var (resp, nomeAcao) = acao.ToolNome switch
                {
                    "clonar_repositorio"      => await ExecutarClone(argsJson),
                    "executar_comandos_pasta" => await ExecutarComandosPasta(argsJson),
                    "abrir_na_ide"            => await ExecutarAbrirIDE(argsJson),
                    "executar_menu"           => await ExecutarMenu(argsJson),
                    "executar_comando_avulso" => ExecutarComandoAvulso(argsJson),
                    _                         => ("Ação desconhecida.", null)
                };
                resultados.Add(resp);
                if (nomeAcao != null) executadas.Add(nomeAcao);
            }
            catch (Exception ex)
            {
                resultados.Add($"Erro em {acao.ToolNome}: {ex.Message}");
            }
        }

        return (string.Join("\n", resultados), executadas);
    }

    // --- Handlers de cada tool ---

    private async Task<(string, string?)> ExecutarClone(string? argsJson)
    {
        var args = ParseArgs(argsJson);
        var config = await configuracaoService.ObterConfiguracaoAsync();

        var criarBranch = GetBool(args, "criarBranchLocal") || GetBool(args, "criarBranchRemoto");
        var codigo = GetString(args, "codigo");
        var descricao = GetString(args, "descricao");
        var branch = GetString(args, "branch");
        var repositorioId = GetGuid(args, "repositorioId");

        var dto = new CloneRequestDTO(
            DiretorioRaiz: config.DiretorioRaiz + "\\",
            Codigo: codigo,
            Descricao: descricao,
            Tipo: GetString(args, "tipo", "nenhum"),
            Branch: branch,
            RepositorioId: repositorioId,
            CriarBranchRemoto: criarBranch,
            BaixarAgregados: GetBool(args, "baixarAgregados"),
            BaixarSomenteAgregados: false
        );

        await cloneService.Clonar(dto, aguardar: true);

        // Cadastra a pasta no banco (mesmo que o CloneController faz no fluxo manual)
        var repositorio = await repositorioJsonService.GetByIdAsync(repositorioId);
        var diretorio = config.DiretorioRaiz + "\\" + codigo + "_" + descricao.Replace(" ", "_");
        var pasta = new PastaCadastroRequestDTO(
            Identificador: Guid.NewGuid(),
            Diretorio: diretorio,
            Codigo: codigo,
            Descricao: descricao,
            Tipo: GetString(args, "tipo", "nenhum"),
            Branch: criarBranch ? codigo : branch,
            Git: repositorio?.Url ?? "",
            RepositorioId: repositorioId
        );
        await pastaService.Cadastrar(pasta);

        return ($"Clonado e registrado! Pasta `{diretorio}` pronta.", "clonar_repositorio");
    }

    private async Task<(string, string?)> ExecutarComandosPasta(string? argsJson)
    {
        var args = ParseArgs(argsJson);

        var projetosNode = args["projetos"]?.AsArray() ?? [];
        var projetos = projetosNode.Select(p => new ProjetoDisponivelDTO(
            Identificador: GetGuid(p?.AsObject(), "identificador"),
            Nome: GetString(p?.AsObject(), "nome"),
            NomeRepositorio: GetString(p?.AsObject(), "nomeRepositorio"),
            Comandos: ParseComandos(p?.AsObject()),
            IdentificadorRepositorioAgregado: null
        )).ToList();

        var dto = new PastaRequestDTO(
            Diretorio: GetString(args, "diretorio"),
            RepositorioId: GetGuid(args, "repositorioId"),
            Projetos: projetos
        );

        await comandoService.ExecutarComando(dto);
        return ("Comandos solicitados com sucesso!", "executar_comandos_pasta");
    }

    private async Task<(string, string?)> ExecutarAbrirIDE(string? argsJson)
    {
        var args = ParseArgs(argsJson);

        var dto = new AbrirPastaIDERequestDTO(
            Diretorio: GetString(args, "diretorio"),
            IDEIdentificador: GetGuid(args, "ideIdentificador")
        );

        await comandoService.AbrirPastaIDE(dto);
        return ("Abrindo na IDE!", "abrir_na_ide");
    }

    private async Task<(string, string?)> ExecutarMenu(string? argsJson)
    {
        var args = ParseArgs(argsJson);

        var dto = new MenuRequestDTO(
            RepositorioId: GetGuid(args, "repositorioId"),
            ComandoId: GetGuid(args, "comandoId"),
            Diretorio: GetString(args, "diretorio")
        );

        await comandoService.ExecutarComandoMenu(dto);
        return ("Menu executado com sucesso!", "executar_menu");
    }

    private (string, string?) ExecutarComandoAvulso(string? argsJson)
    {
        var args = ParseArgs(argsJson);
        var comando = GetString(args, "comando");
        comandoService.ExecutarComandoAvulso(comando);
        return ($"Comando executado: `{comando}`", "executar_comando_avulso");
    }

    private static (string, string?) ExtrairRespostaTexto(string? argsJson)
    {
        var args = ParseArgs(argsJson);
        return (GetString(args, "mensagem", "Ok!"), null);
    }

    // --- Helpers de parse ---

    private static JsonObject ParseArgs(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonNode.Parse(json)?.AsObject() ?? []; }
        catch { return []; }
    }

    private static string GetString(JsonObject? obj, string key, string fallback = "")
        => obj?[key]?.GetValue<string>() ?? fallback;

    private static Guid GetGuid(JsonObject? obj, string key)
    {
        var val = obj?[key]?.GetValue<string>();
        return Guid.TryParse(val, out var guid) ? guid : Guid.Empty;
    }

    private static bool GetBool(JsonObject? obj, string key)
        => obj?[key]?.GetValue<bool>() ?? false;

    private static List<ETipoComando> ParseComandos(JsonObject? obj)
    {
        var arr = obj?["comandos"]?.AsArray();
        if (arr == null) return [];

        return arr
            .Select(c => c?.GetValue<string>())
            .Where(c => c != null)
            .Select(c => Enum.TryParse<ETipoComando>(c, out var e) ? e : (ETipoComando?)null)
            .Where(e => e.HasValue)
            .Select(e => e!.Value)
            .ToList();
    }
}

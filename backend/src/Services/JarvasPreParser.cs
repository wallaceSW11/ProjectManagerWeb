using System.Text;
using System.Text.RegularExpressions;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

/// <summary>
/// Pré-parser em C# puro (sem LLM) que detecta intenções simples e monta
/// a fila de ações diretamente. O LLM só é chamado quando o parser não
/// consegue resolver — reduz drasticamente o tempo de resposta.
/// </summary>
public class JarvasPreParser(
    RepositorioJsonService repositorioService,
    PastaJsonService pastaService,
    ConfiguracaoService configuracaoService)
{
    // Palavras-chave por intenção
    private static readonly string[] _palavrasClone = ["clon", "baixa", "baixar", "clone"];
    private static readonly string[] _palavrasCriarBranch = ["cria", "criar", "nova branch", "new branch", "cria a branch", "criar branch"];
    private static readonly string[] _palavrasIniciar = ["inicia", "iniciar", "start", "sobe", "subir", "roda", "rodar"];
    private static readonly string[] _palavrasInstalar = ["instala", "instalar", "install", "npm i", "npm install"];
    private static readonly string[] _palavrasBuildar = ["build", "builda", "buildar", "compila", "compilar"];
    private static readonly string[] _palavrasAbrirIDE = ["abre", "abrir", "open", "kiro", "vscode", "cursor"];
    private static readonly string[] _palavrasMenu = ["aplica", "aplicar", "apply", "menu", "arquivo", "kiro", "local"];

    public async Task<PreParserResultado?> TentarResolverAsync(string mensagem)
    {
        var msg = mensagem.ToLowerInvariant();
        var repositorios = await repositorioService.GetAllAsync();
        var pastas = await pastaService.GetAllAsync();
        var config = await configuracaoService.ObterConfiguracaoAsync();

        // Detecta repositório mencionado
        var repo = repositorios.FirstOrDefault(r =>
            msg.Contains(r.Titulo.ToLowerInvariant()) ||
            msg.Contains(r.Nome.ToLowerInvariant()));

        // Detecta pasta clonada mencionada (pelo repo ou pelo código)
        var pasta = repo != null
            ? pastas.FirstOrDefault(p => p.RepositorioId == repo.Identificador)
            : null;

        var acoes = new List<AcaoDetectada>();

        // --- CLONE ---
        if (_palavrasClone.Any(msg.Contains) && repo != null)
        {
            var codigo = ExtrairCodigo(msg);
            var descricao = ExtrairDescricao(msg);
            var criarBranch = _palavrasCriarBranch.Any(msg.Contains);
            // Se vai criar branch nova, clona a main/master e faz checkout -b
            // Se não, faz checkout da branch existente
            var branch = criarBranch
                ? (ExtrairBranchBase(msg) ?? "main")
                : (ExtrairBranch(msg) ?? codigo);

            if (string.IsNullOrEmpty(codigo) || string.IsNullOrEmpty(descricao))
                return null; // Deixa pro LLM perguntar

            acoes.Add(new AcaoDetectada("clonar_repositorio", new Dictionary<string, object>
            {
                ["repositorioId"] = repo.Identificador.ToString(),
                ["branch"] = branch!,
                ["codigo"] = codigo.ToUpperInvariant(),
                ["descricao"] = NormalizarDescricao(descricao),
                ["tipo"] = "nenhum",
                ["criarBranchRemoto"] = false,
                ["baixarAgregados"] = false,
                ["criarBranchLocal"] = criarBranch
            }));

            // Diretório que será criado pelo clone (mesmo padrão do CloneService)
            var descNorm = NormalizarDescricao(descricao).Replace(" ", "_");
            var dirClone = $"{config.DiretorioRaiz}\\{codigo.ToUpperInvariant()}_{descNorm}";

            // Detecta menus a aplicar após clone
            var menusParaAplicar = DetectarMenus(msg, repo);
            foreach (var menu in menusParaAplicar)
            {
                acoes.Add(new AcaoDetectada("executar_menu", new Dictionary<string, object>
                {
                    ["diretorio"] = dirClone,
                    ["repositorioId"] = repo.Identificador.ToString(),
                    ["comandoId"] = menu.Identificador.ToString()
                }));
            }

            return new PreParserResultado(acoes, $"Entendido! Vou clonar **{repo.Titulo}** e executar {acoes.Count} ação(ões).");
        }

        // --- INICIAR / INSTALAR / BUILDAR ---
        if (pasta != null)
        {
            var comandos = new List<string>();
            if (_palavrasInstalar.Any(msg.Contains)) comandos.Add("INSTALAR");
            if (_palavrasIniciar.Any(msg.Contains)) comandos.Add("INICIAR");
            if (_palavrasBuildar.Any(msg.Contains)) comandos.Add("BUILDAR");

            if (comandos.Count > 0 && repo != null)
            {
                var projetos = repo.Projetos.Select(p => new
                {
                    identificador = p.Identificador.ToString(),
                    nome = p.Nome,
                    nomeRepositorio = repo.Nome,
                    comandos
                }).ToList<object>();

                acoes.Add(new AcaoDetectada("executar_comandos_pasta", new Dictionary<string, object>
                {
                    ["diretorio"] = pasta.Diretorio,
                    ["repositorioId"] = repo.Identificador.ToString(),
                    ["projetos"] = projetos
                }));

                return new PreParserResultado(acoes, $"Executando {string.Join(", ", comandos)} em **{repo.Titulo}**!");
            }
        }

        // --- MENU AVULSO ---
        if (_palavrasMenu.Any(msg.Contains) && repo != null && pasta != null)
        {
            var menusParaAplicar = DetectarMenus(msg, repo);
            if (menusParaAplicar.Count > 0)
            {
                foreach (var menu in menusParaAplicar)
                {
                    acoes.Add(new AcaoDetectada("executar_menu", new Dictionary<string, object>
                    {
                        ["diretorio"] = pasta.Diretorio,
                        ["repositorioId"] = repo.Identificador.ToString(),
                        ["comandoId"] = menu.Identificador.ToString()
                    }));
                }
                return new PreParserResultado(acoes, $"Aplicando {acoes.Count} menu(s) em **{repo.Titulo}**!");
            }
        }

        // Não conseguiu resolver — deixa pro LLM
        return null;
    }

    // --- Helpers de extração ---

    private static string? ExtrairCodigo(string msg)
    {
        // Padrão: letras-números (ex: TC-940, PMW-123, PROJ-1)
        var match = Regex.Match(msg, @"\b([A-Za-z]{2,6}-\d{1,6})\b");
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    private static string? ExtrairBranch(string msg)
    {
        // "na branch X" ou "branch X" — só para checkout de branch existente
        var match = Regex.Match(msg, @"(?:na\s+)?branch\s+([A-Za-z0-9/_\-]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtrairBranchBase(string msg)
    {
        // "da main", "da master", "a partir da main" — branch base para criar nova
        var match = Regex.Match(msg, @"(?:da|de|from|a partir da?)\s+(main|master|develop|dev)\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "main";
    }

    private static string? ExtrairDescricao(string msg)
    {
        // "com descrição de X" ou "com descricao de X" — captura sem o "de"
        var match = Regex.Match(msg, @"descri[çc][aã]o\s+de\s+(.+?)(?:\s*,|\s*\n|\s+(?:e\s+)?(?:aplica|inicia|baixa|instala|depois|abre|open)|$)", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value.Trim();

        // "descrição X" sem "de"
        match = Regex.Match(msg, @"descri[çc][aã]o\s+(.+?)(?:\s*,|\s*\n|\s+(?:e\s+)?(?:aplica|inicia|baixa|instala|depois|abre|open)|$)", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value.Trim();

        return null;
    }

    private static string NormalizarDescricao(string descricao)
    {
        // Remove acentos
        var normalized = descricao.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        // Remove caracteres especiais, mantém letras, números, espaços e hífens
        return Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"[^a-zA-Z0-9\s\-]", "").Trim();
    }

    private static List<MenuDTO> DetectarMenus(string msg, RepositorioRequestDTO repo)
    {
        var menus = new List<MenuDTO>();
        foreach (var menu in (repo.Menus ?? []).Where(m => m.Ativo))
        {
            var tituloLower = menu.Titulo.ToLowerInvariant();
            // Remove acentos do título para comparação
            var tituloNorm = NormalizarDescricao(tituloLower).ToLowerInvariant();
            var palavrasMenu = tituloNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // Conta quantas palavras significativas (> 3 chars) do título aparecem na mensagem
            var matchCount = palavrasMenu.Count(p => p.Length > 3 && msg.Contains(p));
            // Também verifica se o título inteiro (normalizado) aparece na mensagem
            var tituloNaMsg = msg.Contains(tituloNorm);
            if (matchCount >= 1 || tituloNaMsg)
                menus.Add(menu);
        }
        return menus;
    }
}

public sealed record AcaoDetectada(string ToolNome, Dictionary<string, object> Argumentos);
public sealed record PreParserResultado(List<AcaoDetectada> Acoes, string MensagemInicial);

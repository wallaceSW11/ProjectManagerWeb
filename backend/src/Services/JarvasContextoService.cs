using System.Text;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

/// <summary>
/// Monta o prompt de sistema do JARVAS com dados reais do banco (lidos a cada requisição).
/// Também define as tools disponíveis para o LLM.
/// </summary>
public class JarvasContextoService(
    RepositorioJsonService repositorioService,
    PastaJsonService pastaService,
    IDEJsonService ideService,
    ConfiguracaoService configuracaoService)
{
    public async Task<string> MontarPromptSistemaAsync()
    {
        var repositorios = await repositorioService.GetAllAsync();
        var pastas = await pastaService.GetAllAsync();
        var ides = await ideService.GetAllAsync();
        var config = await configuracaoService.ObterConfiguracaoAsync();

        var sb = new StringBuilder();

        sb.AppendLine("Você é o JARVAS, assistente do Project Manager Web.");
        sb.AppendLine("REGRA ABSOLUTA: Responda SEMPRE usando UMA tool. NUNCA escreva texto livre. NUNCA invente IDs.");
        sb.AppendLine("Se não encontrar algo ou não souber → use resposta_texto.");
        sb.AppendLine();

        sb.AppendLine($"DIR_RAIZ: {config?.DiretorioRaiz ?? "C:\\git"}");
        sb.AppendLine();

        sb.AppendLine("IDEs:");
        foreach (var ide in ides)
            sb.AppendLine($"- {ide.Nome} | {ide.Identificador}");
        sb.AppendLine();

        sb.AppendLine("REPOS:");
        foreach (var repo in repositorios)
            sb.AppendLine($"- {repo.Titulo} | {repo.Identificador}");
        sb.AppendLine();

        sb.AppendLine("PROJETOS (por repo):");
        foreach (var repo in repositorios)
            foreach (var proj in repo.Projetos ?? [])
                sb.AppendLine($"- {repo.Titulo} > {proj.Nome} | {proj.Identificador} | repoId:{repo.Identificador}");
        sb.AppendLine();

        sb.AppendLine("MENUS ATIVOS (por repo):");
        foreach (var repo in repositorios)
            foreach (var menu in (repo.Menus ?? []).Where(m => m.Ativo))
                sb.AppendLine($"- {repo.Titulo} > {menu.Titulo} | {menu.Identificador} | repoId:{repo.Identificador}");
        sb.AppendLine();

        sb.AppendLine("PASTAS CLONADAS:");
        if (pastas.Count == 0)
            sb.AppendLine("- (nenhuma)");
        else
            foreach (var pasta in pastas)
            {
                var repoTitulo = repositorios.FirstOrDefault(r => r.Identificador == pasta.RepositorioId)?.Titulo ?? "?";
                sb.AppendLine($"- {repoTitulo} | dir:{pasta.Diretorio} | repoId:{pasta.RepositorioId} | branch:{pasta.Branch}");
            }
        sb.AppendLine();

        sb.AppendLine("REGRAS DE CLONE:");
        sb.AppendLine("- codigo: maiúsculas (ex: TC-940). Se não informado → resposta_texto pedindo.");
        sb.AppendLine("- descricao: sem acentos nem caracteres especiais. Se não informado → resposta_texto pedindo.");
        sb.AppendLine("- tipo: padrão 'nenhum'.");
        sb.AppendLine("- criarBranchRemoto: padrão false.");

        return sb.ToString();
    }

    public List<LLMToolDTO> ObterTools()
    {
        return
        [
            new LLMToolDTO(
                Nome: "clonar_repositorio",
                Descricao: "Clona um repositório Git com uma branch específica",
                Parametros: new
                {
                    type = "object",
                    properties = new
                    {
                        repositorioId = new { type = "string", description = "ID do repositório (GUID)" },
                        branch = new { type = "string", description = "Nome da branch a fazer checkout" },
                        codigo = new { type = "string", description = "Código da tarefa (ex: TC-940)" },
                        descricao = new { type = "string", description = "Descrição da tarefa" },
                        tipo = new { type = "string", @enum = new[] { "nenhum", "feature", "bug", "hotfix" }, description = "Tipo do prefixo da branch" },
                        criarBranchRemoto = new { type = "boolean", description = "Se deve criar a branch no remoto" },
                        baixarAgregados = new { type = "boolean", description = "Se deve clonar repositórios agregados" }
                    },
                    required = new[] { "repositorioId", "branch", "codigo", "descricao", "tipo", "criarBranchRemoto", "baixarAgregados" }
                }
            ),

            new LLMToolDTO(
                Nome: "executar_comandos_pasta",
                Descricao: "Executa comandos (INICIAR, INSTALAR, BUILDAR, ABRIR_NA_IDE) nos projetos de uma pasta clonada",
                Parametros: new
                {
                    type = "object",
                    properties = new
                    {
                        diretorio = new { type = "string", description = "Diretório completo da pasta clonada" },
                        repositorioId = new { type = "string", description = "ID do repositório (GUID)" },
                        projetos = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    identificador = new { type = "string", description = "ID do projeto (GUID)" },
                                    nome = new { type = "string" },
                                    nomeRepositorio = new { type = "string" },
                                    comandos = new
                                    {
                                        type = "array",
                                        items = new { type = "string", @enum = new[] { "INICIAR", "INSTALAR", "BUILDAR", "ABRIR_NA_IDE" } }
                                    }
                                },
                                required = new[] { "identificador", "nome", "nomeRepositorio", "comandos" }
                            }
                        }
                    },
                    required = new[] { "diretorio", "repositorioId", "projetos" }
                }
            ),

            new LLMToolDTO(
                Nome: "abrir_na_ide",
                Descricao: "Abre um diretório em uma IDE específica",
                Parametros: new
                {
                    type = "object",
                    properties = new
                    {
                        diretorio = new { type = "string", description = "Diretório completo a abrir" },
                        ideIdentificador = new { type = "string", description = "ID da IDE (GUID)" }
                    },
                    required = new[] { "diretorio", "ideIdentificador" }
                }
            ),

            new LLMToolDTO(
                Nome: "executar_menu",
                Descricao: "Executa um menu de um repositório (aplica arquivos, pastas ou comandos)",
                Parametros: new
                {
                    type = "object",
                    properties = new
                    {
                        diretorio = new { type = "string", description = "Diretório da pasta clonada" },
                        repositorioId = new { type = "string", description = "ID do repositório (GUID)" },
                        comandoId = new { type = "string", description = "ID do menu (GUID)" }
                    },
                    required = new[] { "diretorio", "repositorioId", "comandoId" }
                }
            ),

            new LLMToolDTO(
                Nome: "executar_comando_avulso",
                Descricao: "Executa um comando shell arbitrário no PowerShell",
                Parametros: new
                {
                    type = "object",
                    properties = new
                    {
                        comando = new { type = "string", description = "Comando PowerShell a executar" }
                    },
                    required = new[] { "comando" }
                }
            ),

            new LLMToolDTO(
                Nome: "resposta_texto",
                Descricao: "Responde ao usuário com texto sem executar nenhuma ação. Use quando não souber o que fazer, quando algo não for encontrado, ou quando precisar pedir mais informações.",
                Parametros: new
                {
                    type = "object",
                    properties = new
                    {
                        mensagem = new { type = "string", description = "Mensagem de resposta para o usuário" }
                    },
                    required = new[] { "mensagem" }
                }
            )
        ];
    }
}

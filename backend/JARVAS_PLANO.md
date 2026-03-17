# JARVAS — Plano de Implementação
> J.A.R.V.A.S — Just A Rather Very Awesome System (para o PMW)

## Visão Geral

Adicionar ao PMW um assistente de IA local (via Ollama) que permite executar qualquer ação do sistema através de linguagem natural, sem quebrar nada do que já funciona.

O usuário continua usando a interface normal normalmente. O JARVAS é um modal de chat que fica disponível em qualquer tela, e quando ativado, interpreta comandos em português informal e os traduz para chamadas nos endpoints já existentes.

---

## Regras Invioláveis

- Nada do que já funciona pode quebrar
- O JARVAS é **aditivo**, nunca substitutivo
- Segue os padrões de código do front (Vue 3 + Composition API + TypeScript + Vuetify) e do back (C# ASP.NET Core, services injetados, DTOs tipados)
- Sem autenticação nova, sem mudança de banco, sem nova dependência pesada no front
- O Ollama roda **local** — zero cloud, zero custo
- **Os dados do banco (JSON) são a fonte de verdade** — o contexto do LLM é sempre gerado dinamicamente lendo os arquivos, nunca hardcoded
- **O backend é agnóstico ao provedor de LLM** — a interface `ILLMService` abstrai o provedor; trocar de Ollama para GPT/Claude é mudar uma implementação só
- **Toda ação deve ter tratamento de erro explícito** — o JARVAS nunca pode travar ou deixar o usuário sem resposta

---

## Stack do JARVAS

| Componente | Tecnologia | Motivo |
|---|---|---|
| LLM local (padrão) | Ollama + `qwen2.5:7b` | Suporte a function calling, roda local, bom em português |
| Abstração LLM | Interface `ILLMService` no backend | Trocar de provedor sem mexer em mais nada |
| Integração LLM | Novo `JarvasController` no backend C# | Centraliza a lógica, front não precisa saber do provedor |
| Chat UI | Novo componente Vue `JarvasChat.vue` | Botão flutuante + modal, disponível em todas as telas |
| Contexto | Lê `repositorios.json` + `pastas.json` + `IDEs.json` em tempo real | LLM sempre recebe dados atualizados do banco |

---

## Contexto Dinâmico (fonte de verdade = banco JSON)

O `JarvasContextoService` lê os arquivos JSON a cada requisição e monta o prompt de sistema dinamicamente. **Nenhum dado é fixo no código.**

Exemplo do que é gerado em tempo de execução:
```
REPOSITÓRIOS DISPONÍVEIS (lidos do banco agora):
- Título: "Tech Cambio" | Nome: techcambio | id: 58a4b1ab-...
  Projetos: Frontend (id: 753a...), Backend (id: 1d09...), Authenticator (id: e561...), common (id: 6c8f...)
  Menus: "Aplicar arquivos - Local" (id: 1cf5...), "aplicar kiro" (id: 96a3...)
- Título: "Project Manager Web" | Nome: ProjectManagerWeb | id: 27c5...
  Projetos: Frontend (id: 19ad...), Backend (id: ea67...)
... (todos os repositórios cadastrados)

IDEs DISPONÍVEIS (lidas do banco agora):
- Kiro | id: 659d... | comando: kiro .
- VS Code | id: 4c7f... | comando: code .
... (todas as IDEs cadastradas)

PASTAS CLONADAS DISPONÍVEIS (lidas do banco agora):
- Diretório: C:\git\TC-940_Melhoria_no_mobile | Repositório: techcambio | Branch: TC-940
... (todas as pastas cadastradas)

DIRETÓRIO RAIZ: C:\git
```

Isso garante que se amanhã você cadastrar um novo repositório, o JARVAS já sabe dele sem nenhuma mudança de código.

---

## Arquitetura do Fluxo

```
Usuário digita no chat
        ↓
JarvasChat.vue (frontend)
        ↓  POST /api/jarvas/chat
JarvasController.cs (backend)
        ↓  lê banco JSON em tempo real → monta contexto dinâmico
JarvasContextoService.cs
        ↓  envia para o provedor LLM via interface ILLMService
ILLMService → OllamaService (implementação padrão)
        ↓  LLM retorna: tool_call OU resposta_texto OU erro_nao_entendeu
JarvasOrquestrador.cs
        ↓  se tool_call: executa via services existentes
        ↓  se resposta_texto: retorna mensagem diretamente
        ↓  se erro/não encontrado: retorna mensagem amigável de fallback
JarvasController.cs → retorna JarvasChatResponseDTO
        ↓
JarvasChat.vue exibe resposta + chip de ação (se houver)
```

---

## Etapas de Implementação

---

### ETAPA 1 — Backend: Instalar e configurar Ollama

**O que fazer:**
1. Instalar Ollama localmente: https://ollama.com/download
2. Baixar o modelo: `ollama pull llama3.1` (ou `qwen2.5:7b` — melhor em português)
3. Verificar que está rodando em `http://localhost:11434`

**Não mexe em nada do projeto ainda.**

---

### ETAPA 2 — Backend: Interface ILLMService + OllamaService

**Por que interface?**
Amanhã você quer trocar Ollama por GPT-4 ou Claude. Você cria `OpenAIService : ILLMService`, registra no `Program.cs` e pronto. O resto do código não muda nada.

**Criar `backend/src/Services/ILLMService.cs`:**
```csharp
public interface ILLMService
{
    Task<LLMRespostaDTO> EnviarMensagemAsync(LLMRequestDTO request);
    Task<bool> VerificarDisponibilidadeAsync();
}
```

**Criar `backend/src/Services/OllamaService.cs`** — implementa `ILLMService`:
- Chama `http://localhost:11434/api/chat`
- Envia mensagens + tools + contexto
- Deserializa a resposta (tool_call ou content)
- Se o Ollama não estiver rodando, lança exceção tratável

**Contrato de entrada para o Ollama:**
```json
{
  "model": "qwen2.5:7b",
  "messages": [
    { "role": "system", "content": "<contexto dinâmico gerado pelo JarvasContextoService>" },
    { "role": "user", "content": "inicia o tech cambio" }
  ],
  "tools": [ /* definição das tools */ ],
  "stream": false
}
```

**Registrar no `Program.cs`:**
```csharp
builder.Services.AddSingleton<ILLMService, OllamaService>();
// Para trocar: builder.Services.AddSingleton<ILLMService, OpenAIService>();
```

**Tools que o LLM pode chamar:**

| Tool | Descrição | Parâmetros |
|---|---|---|
| `clonar_repositorio` | Clona um repo com branch | repositorioId, branch, codigo, descricao, tipo, criarBranchRemoto, baixarAgregados |
| `executar_comandos_pasta` | Inicia/instala/builda projetos de uma pasta | diretorio, repositorioId, projetos[{identificador, comandos[]}] |
| `abrir_na_ide` | Abre pasta em uma IDE | diretorio, ideIdentificador |
| `executar_menu` | Aplica arquivos/pastas de um menu | diretorio, repositorioId, comandoId |
| `executar_comando_avulso` | Executa qualquer comando shell | comando |
| `listar_pastas` | Lista pastas disponíveis | (sem parâmetros) |
| `resposta_texto` | Responde sem executar nada | mensagem |

---

### ETAPA 3 — Backend: JarvasContextoService

Criar `backend/src/Services/JarvasContextoService.cs`

Responsabilidade: **ler o banco JSON em tempo real** e montar o prompt de sistema. Nunca usa dados fixos no código.

```csharp
public async Task<string> MontarContextoAsync()
{
    var repositorios = await repositorioJsonService.GetAllAsync();
    var pastas = await pastaJsonService.GetAllAsync();
    var ides = await ideJsonService.GetAllAsync();
    var config = await configuracaoService.GetAsync();

    // Monta string de contexto com todos os dados reais
    // Inclui: ids, títulos, nomes, projetos, menus, IDEs, diretórios
}
```

O prompt instrui o LLM a:
- Sempre usar os IDs exatos do contexto ao chamar tools
- Usar `resposta_texto` quando não souber o que fazer
- Usar `resposta_texto` quando não encontrar o que o usuário pediu (ex: repositório não cadastrado)
- Responder em português informal
- Nunca inventar IDs ou dados que não estejam no contexto

---

### ETAPA 4 — Backend: JarvasOrquestrador

Criar `backend/src/Services/JarvasOrquestrador.cs`

Responsabilidade: receber a tool call decidida pelo LLM e chamar o service correto já existente. **Também é responsável por todo o tratamento de erro e fallback.**

```csharp
switch (toolCall.Name)
{
    case "clonar_repositorio":
        await cloneService.Clonar(dto);
        break;
    case "executar_comandos_pasta":
        await comandoService.ExecutarComando(dto);
        break;
    case "abrir_na_ide":
        await comandoService.AbrirPastaIDE(dto);
        break;
    case "executar_menu":
        await comandoService.ExecutarComandoMenu(dto);
        break;
    case "executar_comando_avulso":
        comandoService.ExecutarComandoAvulso(dto.Comando);
        break;
    case "resposta_texto":
        // LLM respondeu sem executar ação — retorna direto
        break;
    default:
        // Tool desconhecida — retorna fallback
        break;
}
```

**Camadas de proteção:**

| Cenário | O que acontece |
|---|---|
| LLM offline (Ollama não está rodando) | Retorna: "Eita, o JARVAS está dormindo. Verifique se o Ollama está rodando." |
| LLM não entendeu o comando | LLM usa `resposta_texto` com "Não entendi o que você quis dizer. Pode reformular?" |
| LLM inventou um ID que não existe | Service lança exceção → orquestrador captura → retorna mensagem amigável |
| Repositório/pasta não encontrado no banco | LLM usa `resposta_texto` com "Não encontrei nenhum repositório com esse nome. Os disponíveis são: ..." |
| Erro na execução do comando shell | Captura exceção → retorna "Comando solicitado, mas algo deu errado na execução." |
| Timeout do LLM (> 30s) | CancellationToken → retorna "O JARVAS demorou demais pra pensar. Tenta de novo." |
| Resposta malformada do LLM | Try/catch no parse → fallback genérico |

---

### ETAPA 5 — Backend: DTOs e Controller

**Criar `backend/src/DTOs/JarvasDTOs.cs`:**
```csharp
public sealed record JarvasChatRequestDTO(
    string Mensagem,
    List<JarvasMensagemDTO>? Historico
);

public sealed record JarvasMensagemDTO(
    string Role,  // "user" | "assistant"
    string Conteudo
);

public sealed record JarvasChatResponseDTO(
    string Resposta,
    string? AcaoExecutada,  // ex: "clonar_repositorio", null se só respondeu
    bool Sucesso,
    string? ErroDetalhes    // preenchido só quando Sucesso = false
);

// DTO interno para comunicação entre ILLMService e Orquestrador
public sealed record LLMRequestDTO(
    string PromptSistema,
    List<JarvasMensagemDTO> Historico,
    List<LLMToolDTO> Tools
);

public sealed record LLMRespostaDTO(
    bool EhToolCall,
    string? ToolNome,
    string? ToolArgumentosJson,
    string? TextoResposta
);
```

**Criar `backend/src/Controllers/JarvasController.cs`:**
```
POST /api/jarvas/chat    ← mensagem do usuário → resposta do JARVAS
GET  /api/jarvas/status  ← verifica se o provedor LLM está disponível
```

O controller **nunca lança exceção para o front** — sempre retorna `JarvasChatResponseDTO` com `Sucesso = false` e uma mensagem amigável quando algo dá errado.

---

### ETAPA 6 — Frontend: JarvasService

Criar `frontend/src/services/JarvasService.ts`

Seguindo o padrão do `BaseApiService.ts`:
```typescript
class JarvasService extends BaseApiService {
  async chat(mensagem: string, historico: MensagemChat[]): Promise<JarvasResponse>
  async verificarStatus(): Promise<boolean>
}
```

**Tipos novos em `frontend/src/types/index.ts`:**
```typescript
export interface MensagemChat {
  role: 'user' | 'assistant';
  conteudo: string;
  timestamp: Date;
}

export interface JarvasResponse {
  resposta: string;
  acaoExecutada?: string;
  sucesso: boolean;
}
```

---

### ETAPA 7 — Frontend: JarvasChat.vue

Criar `frontend/src/components/jarvas/JarvasChat.vue`

**Características visuais (seguindo padrão Vuetify dark do projeto):**
- Botão flutuante no canto inferior direito (FAB) com ícone `mdi-robot` na cor primary (`#FF5533`)
- Badge vermelho no FAB quando o JARVAS está offline (status do `/api/jarvas/status`)
- Ao clicar, abre um `v-dialog` de chat estilo messenger
- Histórico de mensagens com scroll automático para o fim
- Input de texto na parte inferior com envio por Enter
- Indicador de "digitando..." (3 pontinhos animados) enquanto aguarda resposta
- Chip colorido mostrando a ação executada (ex: `✓ clone_repositorio`)
- Botão para limpar histórico

**Estados de erro no chat (nunca deixa o usuário sem resposta):**
- JARVAS offline → mensagem no chat: "Eita, estou dormindo. Verifique se o Ollama está rodando."
- Não entendeu → mensagem no chat com sugestão de reformular
- Não encontrou → mensagem no chat listando o que está disponível
- Erro de execução → mensagem no chat informando o problema

**Integração com eventBus existente:**
- Usa `notificar()` para erros críticos (falha de rede total)
- Usa `atualizarListaPastas()` após ações que criam/modificam pastas (clone, etc.)
- NÃO usa `carregando()` global — o chat tem seu próprio indicador de "digitando"

---

### ETAPA 8 — Frontend: Integrar JarvasChat no App.vue

Adicionar o componente `<JarvasChat />` no `App.vue`, logo após o `<SitesGerenciamento />`.

**Apenas isso. Nenhuma outra mudança no App.vue.**

---

### ETAPA 9 — Exemplos de comandos que devem funcionar

Após a implementação, esses comandos devem funcionar:

```
"inicia o tech cambio"
→ busca pasta com repositório "techcambio", executa INICIAR em todos os projetos

"clona o tech cambio na branch TC-940 com descrição Melhoria no mobile"
→ CloneService.Clonar com os parâmetros corretos

"aplica os arquivos locais no tech cambio"
→ executa o menu "Aplicar arquivos - Local" na pasta ativa

"aplica o kiro no tech cambio"
→ executa o menu "aplicar kiro"

"abre o front do PMW no kiro"
→ abre o projeto Frontend do ProjectManagerWeb na IDE Kiro

"instala o month balance"
→ executa INSTALAR nos projetos do MonthBalance

"abre o diretório do tech cambio"
→ executar_comando_avulso: explorer no diretório

"qual pasta está selecionada?"
→ resposta_texto com informação das pastas disponíveis
```

---

## Ordem de Execução Recomendada

1. **ETAPA 1** — Instalar Ollama (manual, fora do código)
2. **ETAPA 2** — OllamaService (backend)
3. **ETAPA 3** — JarvasContextoService (backend)
4. **ETAPA 4** — JarvasOrquestrador (backend)
5. **ETAPA 5** — DTOs + JarvasController (backend)
6. Testar o backend via `.http` ou Postman antes de mexer no front
7. **ETAPA 6** — JarvasService (frontend)
8. **ETAPA 7** — JarvasChat.vue (frontend)
9. **ETAPA 8** — Integrar no App.vue
10. Testar end-to-end

---

## Dependências Novas

**Backend:**
- Nenhum pacote NuGet novo — usa `HttpClient` nativo do .NET para chamar o Ollama

**Frontend:**
- Nenhum pacote npm novo — usa `axios` já instalado via `BaseApiService`

**Infraestrutura:**
- Ollama instalado localmente (download único, ~4GB para o modelo)

---

## O que NÃO vai mudar

- Nenhum controller existente
- Nenhum service existente
- Nenhum componente Vue existente
- Nenhuma rota do router
- Nenhum arquivo de configuração
- Banco de dados (JSON files) — apenas leitura pelo contexto
- Comportamento atual da aplicação

---

## Notas Técnicas

**Por que interface `ILLMService` e não chamar Ollama direto?**
Amanhã você quer trocar por GPT-4, Claude ou qualquer outra API. Você cria `OpenAIService : ILLMService`, troca uma linha no `Program.cs` e pronto. O `JarvasOrquestrador`, o `JarvasController` e o frontend não mudam nada.

**Por que não streaming?**
Para começar simples e estável. Pode ser adicionado depois com `stream: true` no Ollama e SSE no frontend — sem quebrar nada.

**Modelo recomendado:**
`qwen2.5:7b` — melhor compreensão de português, bom em function calling, roda bem em 8GB RAM.
Alternativa: `llama3.1:8b` — mais popular, também funciona.

**Histórico de conversa:**
O frontend mantém o histórico em memória (não persiste entre sessões). Cada mensagem nova envia o histórico completo para o backend, que repassa ao LLM como contexto de conversa.

**Timeout:**
`OllamaService` usa `CancellationToken` com 30 segundos. Se o modelo demorar mais que isso, o usuário recebe uma mensagem amigável e pode tentar de novo.

**Contexto dinâmico:**
O `JarvasContextoService` lê os JSONs a cada requisição. Não tem cache intencional — os dados são pequenos e isso garante que o LLM sempre veja o estado atual do banco.

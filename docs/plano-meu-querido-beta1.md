# Plano — Meu Querido Beta 0000001 (v0.1, Linux)

> Documento de decisão e arquitetura. Data: 30/10/2026.
> Escopo desta versão: **Linux apenas** (Windows fica para depois, com as pendências listadas em "Limitações").
> Backlog executável: `docs/tarefas-meu-querido-beta1.md`.
> Nada deste documento está implementado.

## 1. Status do documento

| Item | Estado |
|-|-|
| Investigação do PMW | concluída no código |
| Investigação do OpenCode | concluída nesta máquina (v2.2.5, somente leitura — nenhuma sessão criada, nenhum prompt enviado) |
| Decisões de arquitetura (A–E) | **todas aprovadas** (A .NET, B polling, C `ask`/`deny`, D SQLite, E PMW em Development) — ver seção 2 |
| Implementação | não iniciada |
| Alterações em produção | nenhuma |

## 2. Decisões

| # | Decisão | Escolha | Estado |
|-|-|-|-|
| A | Onde vive o Core | Backend .NET, módulo `Services/MeuQuerido/` | aprovado |
| B | Progresso até a UI | Polling de ~1s enquanto executa | aprovado |
| C | Permissões do agente | `ask` para exclusão e para shell fora da allowlist; `deny` para `git commit`/push/merge/rebase/reset/clean/tag/`gh` | aprovado |
| D | Persistência | SQLite desde o beta (`meuquerido.db`), preparando memória e auditoria | aprovado |
| E | Ambiente de demonstração | O próprio PMW rodando em Development (`Banco_Dev`), sem tocar produção | aprovado, com cuidados na seção 2.3 |

### 2.1 Por que polling (B)

`POST /api/session/{id}/prompt` é assíncrono: enfileira a mensagem e retorna imediatamente. Sem um mecanismo de consulta, a UI não sabe se a execução terminou, o que foi executado, nem se há permissão pendente.

| Opção | Custo | Decisão |
|-|-|-|
| Polling de `GET /api/meu-querido/conversa/{projetoId}` a cada ~1s, só durante execução | 1 requisição local/segundo; sem reconexão nem estado no browser; depurável com `curl` | **recomendado** |
| WebSocket no padrão do monitoramento | hub, reconexão, sincronização de estado; mais peças no beta | adiado |
| SSE do navegador direto no OpenCode | exigiria expor a senha do serviço ao navegador e tiraria o controle do PMW | descartado |

O contrato não muda entre polling e push: `GET /conversa` é a fonte de verdade. Trocar o transporte depois (voz, por exemplo) não altera o backend.

### 2.2 Persistência em SQLite desde o beta (D)

Decidido: o Meu Querido nasce com banco próprio em SQLite (`meuquerido.db`, em `PathHelper.BancoPath`), em vez de mais um JSON. Motivo: o Meu Querido vai acumular estado que não é lista simples — sessão por projeto, histórico de operações, e depois memória, preferências e padrões. Começar certo custa pouco agora e evita uma migração de dados logo depois.

**Por que isso não quebra o padrão do projeto:** o PMW usa JSON para dados de cadastro (poucos registros, leitura inteira em memória). O Meu Querido é log/estado com consulta por chave e por período. Os dois convivem sem se misturar: SQLite fica isolado no módulo, nenhum `JsonService` existente muda.

**Dependência:** `Microsoft.Data.Sqlite` (inclui SQLite nativo; validar `dotnet publish` self-contained em `linux-x64`).

**Esquema inicial (v1)**

| Tabela | Colunas | Para quê |
|-|-|-|
| `sessoes` | `id` (PK), `projeto_id`, `diretorio`, `opencode_session_id`, `agente`, `modelo`, `criado_em`, `atualizado_em` | reaproveitar a sessão do OpenCode por projeto e sobreviver a restart/update |
| `operacoes` | `id` (PK), `sessao_id`, `tipo`, `alvo`, `resultado`, `detalhe`, `criado_em` | auditoria: o que foi pedido, onde, o que aconteceu (sem segredo) |
| `schema_version` | `versao`, `aplicado_em` | versionamento do próprio esquema |

**O que não vai para o banco**

- histórico de mensagens: continua no OpenCode, que é a fonte de verdade da conversa; duplicar gera desincronização e custo;
- qualquer token, chave SSH, conteúdo de `.env` ou payload bruto de ferramenta.

**Migrations:** DDL versionado dentro do módulo (`CREATE TABLE IF NOT EXISTS` + versão aplicada em `schema_version`), executado na primeira inicialização. Não usa `MigrationService` de JSON nem `dotnet ef` — o banco é do Meu Querido, não do PMW.

**Regras de acesso:** uma conexão por operação (`using`), `WAL` ligado, sem cache estático de conexão. Escrita sempre em transação curta.

Critérios para evoluir o esquema depois (memória, personalidade, padrões): novas tabelas e nova versão, nunca `ALTER` destrutivo.

### 2.3 Testar no próprio PMW (E)

Rodar em Development é seguro para os dados: `PathHelper.Configure` troca o diretório do banco para `Banco_Dev` quando o ambiente é `Development` (`backend/src/Utils/PathHelper.cs`), então `repositorios.json`, `pastas.json` e `Configuracao.json` de produção não são tocados.

Cuidados obrigatórios, porque o repositório de teste é o mesmo onde o Meu Querido está sendo implementado:

- o agente edita **este** working tree; antes de cada execução o PMW mostra `git status --porcelain`, e o preâmbulo informa o que já estava modificado;
- instruções de teste são delimitadas: criar arquivo novo em `docs/` ou na raiz, nunca refatorar;
- `git commit` é `deny` — a revisão sempre acontece antes, fora do Meu Querido;
- subir a instância de teste em porta alternativa (`ASPNETCORE_URLS`, `--no-launch-profile`), sem derrubar a instância do usuário na 2024/2025;
- medir o resultado com `git -C . log --oneline -1` inalterado e `git status` contendo apenas o arquivo criado.

## 3. Objetivo e hipótese

Hipótese a validar:

> O usuário conversa com o Meu Querido e, por meio dele, dispara uma operação real em um projeto local, usando o PMW como fonte de verdade e o OpenCode como executor — **sem tocar no terminal do OpenCode**.

O beta **não** é um agente novo e **não** é um editor. É uma camada de orquestração entre:

- a UI do PMW (Vue),
- os dados reais do PMW (projetos, caminhos, repositórios),
- a sessão de um agente já configurado no OpenCode (modelo do usuário, sem chave nova).

### 3.1 Visão do produto e roadmap

Objetivo de produto declarado: **um chat que prepara o projeto (clonar ou iniciar) e implementa o que foi pedido**, seguindo a documentação da tarefa ou a instrução do usuário na conversa.

O Meu Querido v0.1 entrega apenas o miolo dessa visão: conversa com contexto real do projeto e execução de implementação no diretório correto, com revisão antes do commit. O restante entra em incrementos, cada um reusando o que o PMW já tem:

| Incremento | Entrega | O que já existe no PMW | Risco principal |
|-|-|-|-|
| **v0.1 (este plano)** | selecionar projeto conhecido → conversar → implementar → revisar | `PastaService`, `GitCommandRunner`, `ComandoService.AbrirPastaIDE` | agente editar arquivos sem revisão (controlado por `ask`/`deny` + painel de revisão) |
| v0.2 | iniciar/parar o ambiente do projeto pelo chat ("inicie o front") | `ComandoService.ExecutarComando` (`INICIAR`/`INSTALAR`/`BUILDAR`), `TerminalService` | processos longos, porta ocupada, comando arbitrário |
| v0.3 | clonar por código de tarefa e já abrir sessão no clone | `CloneService.Clonar`, `PastaService.Cadastrar`, `GET /api/repositorios/codigos-tarefa/{iniciais}` | rede/credenciais, URL errada, branch errado, clonar em diretório existente |
| v0.4 | ler a documentação da tarefa (gestor externo) e transformar em plano | só existe `UrlBaseGestorTarefas` como URL cadastrada | sem integração real; exige definir a fonte |
| v0.5 | memória, personalidade e padrões | base SQLite criada na v0.1 | qualidade do contexto, custo por mensagem |

Por que não juntar clonar/iniciar na v0.1: as duas escrevem fora do diretório do agente (rede, disco, processos) e exigem confirmação explícita com resumo do que será feito. Misturar isso com a validação do fluxo conversacional esconde qual parte falhou quando algo der errado — justamente o que o beta precisa isolar.

Se você quiser antecipar v0.3 para a v0.1, o custo é: tela de confirmação de clone, validação de destino/branch e tratamento de falha de rede — duas tarefas a mais, sem provar nada novo sobre a hipótese central.

## 4. Diagnóstico do PMW — o que existe de fato

### 4.1 Camadas e entrega

| Fato | Evidência |
|-|-|
| .NET 9 + ASP.NET Core, sem EF e sem AutoMapper | `backend/ProjectManagerWeb.csproj` |
| Backend serve a SPA estática e faz fallback para `index.html` | `backend/Program.cs:8-11`, `:129-152` |
| CORS liberado (`AllowAnyOrigin`) e **sem autenticação** | `backend/Program.cs:15-19` |
| Tudo singleton no DI | `backend/Program.cs:29-84` |
| Release por OS, binário self-contained; instalação baixa zip | `bootstrap.sh:66-119`; `.github/workflows/release.yml` |
| Persistência em JSON por máquina, `SemaphoreSlim(1,1)` por serviço | `backend/src/Utils/PathHelper.cs`; `backend/src/Services/RepositorioJsonService.cs` |
| Arquivos gravados em **PascalCase**; API responde **camelCase** | `Banco_Dev/Configuracao.json` (`DiretorioRaiz`) × `types/index.ts` (`diretorioRaiz`) |

### 4.2 Como um projeto é resolvido

- `PastaService.ObterTodas()` (`backend/src/Services/PastaService.cs:9`) varre `DiretorioRaiz` e as pastas centralizadoras, cruza com `pastas.json` e devolve `PastaResponseDTO`.
- Pasta no disco **sem cadastro** volta com `RepositorioId = Guid.Empty` — não é projeto utilizável.
- Regra canônica do diretório de trabalho do repositório: `Path.Combine(pasta.Diretorio, repositorio.Nome)` — `backend/src/Services/ComandoService.cs:16`.
- Subdiretório do projeto: `backend/src/Services/ComandoService.cs:22` e `:76`.
- Código de tarefa → repositório: `GET /api/repositorios/codigos-tarefa/{iniciais}` (`RepositorioController`), consumido pelo fluxo de clone (`CloneService.Clonar`, `backend/src/Services/CloneService.cs:51`).

Exemplo real da máquina de desenvolvimento:

```
DiretorioRaiz              /home/wallace/git
PastaCentralizadora        pessoal
Pasta (pastas.json)        /home/wallace/git/pessoal/PMWMQ_Meu_Querido
Repositorio.Nome           ProjectManagerWeb
Diretório de trabalho      /home/wallace/git/pessoal/PMWMQ_Meu_Querido/ProjectManagerWeb
```

### 4.3 Como o PMW executa coisas

- `IShellProvider` (`backend/src/Utils/IShellProvider.cs`): terminal com/sem interface, administrador, abrir diretório/arquivo; implementações `WindowsShellProvider` / `LinuxShellProvider`.
- `ComandoService`: `ExecutarComando`, `ExecutarComandoAvulso`, `ExecutarComandoMenu`, `AbrirPastaIDE` (`ComandoService.cs:469`).
- `GitCommandRunner.RunAsync(comando, sshKey)` (`backend/src/Services/GitCommandRunner.cs`): executa string via `git` ou `bash -c`, devolve stdout/stderr/exit code. **Não aceita `WorkingDirectory`** — o diretório precisa ir no comando (`git -C ...`).

Consequência: para leitura de git o Meu Querido **reaproveita** `IGitCommandRunner`; não há executor novo a criar.

### 4.4 Tempo real já existente

- WebSocket cru: `GET /api/monitoramento/ws` (`backend/src/Controllers/MonitoramentoController.cs:10`), com `UseWebSockets()` em `Program.cs:130`.
- Front consome com reconexão exponencial e callbacks (`frontend/src/services/monitoramentoService.ts`) e store Pinia Options (`frontend/src/stores/monitoramento.ts`).

### 4.5 Padrões a seguir na implementação

- Fluxo obrigatório: Component → Store → Service → API | Controller → Service → JsonService.
- Backend: controller fino, primary constructor, early return, sem comentários, DTO para tudo.
- Frontend: Options API no Pinia, `.then` proibido, loading no `finally`, template sem lógica, componente ≤ 200 linhas.
- Boilerplates: `.opencode/referencias/backend-boilerplates.md` e `frontend-boilerplates.md`.

## 5. Diagnóstico do OpenCode — o que foi verificado

Verificado em 30/10/2026, nesta máquina, apenas com chamadas de leitura.

| Fato | Como foi verificado |
|-|-|
| Versão 2.2.5, binário em `/home/wallace/.opencode/bin/opencode` | `opencode --version`, `which opencode` |
| Serviço compartilhado ativo em `http://127.0.0.1:49374` (pid 26969) | `opencode service status`, `GET /api/info` |
| Registro do serviço em `~/.local/state/opencode/service.json` com `{id, version, url, pid, password}` | `opencode debug paths state` |
| Autenticação HTTP Basic, usuário fixo `opencode`, senha do registro | `200` com `-u opencode:<senha>`; `401 UnauthorizedError` sem |
| OpenAPI disponível em `/openapi.json` | `opencode api get /openapi.json` (o `curl` direto exige auth) |
| Criar sessão com diretório de trabalho | `POST /api/session` aceita `location: { directory }` |
| Qualquer diretório existente é aceito; inexistente retorna `LocationNotFoundError` | `GET /api/model/default?location[directory]=...` em `/home/wallace/git/pessoal` e em `/nao/existe` |
| Parâmetro de localização é objeto serializado | `location[directory]=<caminho absoluto>` — `location=<path>` retorna `InvalidRequestError` |
| Enviar instrução | `POST /api/session/{id}/prompt` com `{ text }`; retorna imediatamente (mensagem entra na inbox) |
| Ler conversa | `GET /api/session/{id}/message?limit&order&cursor` — partes `text`, `reasoning`, `tool` com `state.status` em `streaming/running/completed/error` |
| Saber se está executando / terminou | `GET /api/session/{id}` — `time.busy`, `time.idle`, `outcome` (`succeeded/failed/interrupted`) |
| Permissões pendentes | `GET /api/permission/request`; resposta em `POST /api/session/{id}/permission/{rid}/reply` `{decision: once|always|reject}` |
| Interromper | `POST /api/session/{id}/interrupt` |
| Git | `GET /api/vcs/status`, `GET /api/vcs/diff` |
| Eventos | `GET /api/event` (SSE, `data: {"id","type","data"}`); `server.connected` observado |
| Modelo e agente resolvidos por diretório | `GET /api/model/default`, `GET /api/agent` — leem o `opencode.json` do projeto |
| Modelos disponíveis sem chave nova | `opencode models` lista `opencode-go/*` e `deepseek/*` (credenciais do usuário em `~/.local/share/opencode/auth.json`) |
| API marcada como experimental | `/openapi.json` → `info.version 0.0.1`, descrição "Experimental HttpApi surface" |

Formato das permissões (documentação V2 + `opencode.json` do próprio PMW): lista de `{action, resource, effect}` com `effect` em `allow | deny | ask`. Ações: `read`, `edit`, `glob`, `grep`, `shell`, `subagent`, `skill`, `question`, `webfetch`, `websearch`, `external_directory`, `<mcp>_<tool>`, `execute`.

Ponto crítico: **`external_directory` tem política padrão permissiva**. Não podemos depender do default para conter o agente dentro do projeto.

## 6. Arquitetura proposta

```
┌─────────────────────────────┐
│  Vue — MeuQueridoView       │  chat, progresso, aprovação, revisão
└──────────────┬──────────────┘
               │ HTTP /api/meu-querido/*  (nunca fala com o OpenCode)
┌──────────────▼──────────────┐
│  PMW backend (.NET)         │
│  ├─ MeuQueridoService       │  orquestra: projeto → sessão → prompt → snapshot
│  ├─ ProjetoLocalService     │  valida projeto e caminho (fonte: pasta + repositório)
│  ├─ RevisaoService          │  git status/diff via IGitCommandRunner
│  ├─ MeuQueridoJsonService   │  mapa diretório → sessionID
│  └─ OpenCodeClient          │  descoberta + Basic + endpoints /api/*
└──────┬───────────────┬──────┘
       │ JSON/git      │ HTTP Basic 127.0.0.1
┌──────▼──────┐  ┌─────▼──────────────────┐
│ PMW Banco/  │  │ OpenCode 2.2.5 (serviço)│  sessão + modelo do usuário
└─────────────┘  └─────────────────────────┘
```

### Responsabilidades

| Componente | Faz | Não faz |
|-|-|-|
| `MeuQueridoView` | lista projetos, envia mensagem, mostra progresso/aprovação/revisão | não conhece OpenCode, não recebe senha, não monta caminho |
| `MeuQueridoService` | ciclo de sessão, preâmbulo de contexto, snapshot para a UI, interrupção | não executa git de escrita, não decide regra de projeto |
| `ProjetoLocalService` | `projetoId` → diretório absoluto validado + metadados enxutos | não lista diretórios do disco por conta própria (usa `PastaService`) |
| `OpenCodeClient` | descoberta, auth, create/prompt/messages/session/permission/interrupt | não contém regra de negócio do PMW |
| `RevisaoService` | branch atual, arquivos alterados, diffstat | não faz commit, checkout, reset |
| `OpenCode` | executa a tarefa com o modelo do usuário | não é fonte de verdade sobre projetos |

### Por que .NET e não Node (recomendação A)

1. Distribuição: a release é binário .NET self-contained (`bootstrap.sh` não instala Node). Serviço Node exigiria nova dependência de instalação ou empacotamento extra.
2. Reuso: `HttpClient`, DI, `SemaphoreSlim`, WebSocket, xUnit e CI já existem; um cliente HTTP + SSE de ~200 linhas cobre tudo que o beta precisa.
3. Segurança: a senha do OpenCode nunca sai do backend; uma porta a menos exposta; nenhum novo processo para supervisionar.

O que se perde: os tipos prontos de `@opencode/client` (npm 2.2.5). Isso é conveniência, não capacidade. Reavaliar apenas se o Core crescer para um planejador próprio ou hospedar MCP.

### Por que a sessão do OpenCode é o "cérebro" (recomendação embutida)

O beta **não** implementa laço de plano próprio. A conversa é a sessão do OpenCode:

- o modelo do usuário responde (sem chave nova, sem custo de infraestrutura);
- o histórico é mantido pelo OpenCode entre mensagens e entre reinícios do PMW;
- ferramentas, permissões, snapshots e diffs já existem do lado dele.

O Core envia, junto da primeira mensagem de cada sessão, um preâmbulo curto com: projeto, repositório, diretório, branch atual, arquivos já modificados antes do início e as proibições (não commitar, não pushar, não resetar, não apagar). Nada de dump de DTO, nada de token/chave.

Um planejador próprio (roteamento de intenção com `POST /api/experimental/generate/text`) fica fora: adiciona um segundo agente, outro contrato de prompt e dependência de endpoint experimental, sem provar nada a mais nesta etapa.

### Sobre MCP

Fora do beta. No fluxo atual o PMW injeta contexto explícito; não há necessidade de o modelo descobrir ferramentas em tempo real. MCP passa a valer quando: (1) o modelo precisar consultar muitos projetos dinamicamente, (2) clientes externos (TUI, outros editores) também precisarem de ferramentas do PMW, ou (3) o catálogo de ferramentas crescer além do que cabe no prompt. Nenhuma das três existe no Beta 0000001.

## 7. Contratos da API do Meu Querido

Proposta (nomes finais na implementação, seguindo o padrão de `Controllers/`).

```
GET  /api/meu-querido/status                  → { disponivel, versao, motivo }
GET  /api/meu-querido/projetos                → [ProjetoLocalDTO]
GET  /api/meu-querido/conversa/{projetoId}    → ConversaResponseDTO
POST /api/meu-querido/mensagens               → { projetoId, texto }
POST /api/meu-querido/interromper             → { projetoId }
POST /api/meu-querido/permissoes/responder    → { projetoId, requestId, decisao }
GET  /api/meu-querido/revisao/{projetoId}     → RevisaoResponseDTO
```

Regras de contrato:

- `projetoId` é identificador que o PMW já conhece — **nunca** um caminho vindo do cliente.
- `ProjetoLocalDTO` expõe apenas: identificador, título, repositório, código da pasta, diretório de trabalho, branch atual, indicador de alterações pendentes. **Nunca** `GitHubToken` nem `CaminhoChaveSSH`.
- Mensagens normalizadas: `{ papel, texto, ferramenta?, status?, resumo? }`. Payload bruto de ferramenta não é repassado à UI.
- `ConversaResponseDTO`: `{ sessionId, estado: ocioso|executando|aguardandoPermissao|indisponivel|erro, agente, modelo, mensagens[], permissaoPendente?, erro? }`.
- `RevisaoResponseDTO`: `{ branch, arquivos[], resumoDiff, commitRealizado: false }`.

## 8. Contrato com o OpenCode

### 8.1 Descoberta e autenticação

1. Localizar o binário: `opencode` no `PATH`; fallback `~/.opencode/bin/opencode`.
2. Localizar o estado: `opencode debug paths state` → diretório do registro (validado: `/home/wallace/.local/state/opencode`). O arquivo é `service.json` no canal padrão e pode ser `service-<canal>.json` em outros canais.
3. Ler `{url, pid, password, version}` do registro.
4. Validar com `GET /api/info` (Basic `opencode:<password>`, timeout curto): confere se está saudável e se a versão começa com `2.`.
5. Sem registro ou `/api/info` falhando: `estado = indisponivel` com motivo; UI oferece ação explícita "iniciar OpenCode" (`opencode service start`) e relê o registro. Não reiniciar o serviço automaticamente — ele é compartilhado com a TUI do usuário.

Nunca usar caminho fixo de estado e nunca reimplementar autenticação: o registro é a fonte.

### 8.2 Endpoints usados no beta

| Operação | Endpoint |
|-|-|
| Saúde/versão | `GET /api/info` |
| Criar sessão | `POST /api/session` `{ location: { directory }, permissions }` |
| Enviar instrução | `POST /api/session/{id}/prompt` `{ text }` |
| Ler conversa | `GET /api/session/{id}/message?limit=&order=` |
| Estado da sessão | `GET /api/session/{id}` |
| Permissão pendente | `GET /api/permission/request` |
| Responder permissão | `POST /api/session/{id}/permission/{rid}/reply` |
| Interromper | `POST /api/session/{id}/interrupt` |
| Listar sessões do diretório (recuperação) | `GET /api/session?directory=` |

Fora do beta: `/api/event` (SSE), `/api/vcs/*`, `/api/worktree/*`, `/api/agent`, `/api/model`, `/api/experimental/*`.

### 8.3 Timeouts e falhas

| Situação | Comportamento |
|-|-|
| `/api/info` lento ou mudo | 3s → `indisponivel` |
| `POST /prompt` falha | erro na conversa, estado `erro`, nenhuma alteração feita |
| Execução sem terminar | 10 min → `interrupt` + mensagem de timeout |
| Permissão sem resposta | 5 min → `interrupt` + mensagem, nunca aprovação automática |
| Sessão não existe mais (apagada na TUI) | recria e avisa na conversa |
| Serviço cai no meio | estado `indisponivel`; o usuário reexecuta a mensagem |
| PMW reinicia | mapa JSON recupera `sessionID`; execução em andamento é retomada como somente leitura |

## 9. Segurança

Princípio: **a restrição não pode depender do modelo**. Três camadas independentes.

### 9.1 Caminho (código do PMW)

- `ProjetoLocalService` normaliza o caminho final e exige que esteja contido em `DiretorioRaiz` ou em uma pasta centralizadora cadastrada.
- Exige `Directory.Exists` e `.git` presente.
- Entrada do cliente é identificador, nunca caminho.
- Sem `DiretorioRaiz` configurado → lista vazia, sem exceção.

### 9.2 Permissões do OpenCode (por sessão, definidas na criação)

Política aprovada: execução livre só para inspeção; **tudo que destrói ou sai do projeto é perguntado**; **commit e push são proibidos**.

| Ação / recurso | Efeito | Por quê |
|-|-|-|
| `external_directory` `*` | `deny` | default do OpenCode é permissivo; contenção explícita no diretório do projeto |
| `shell` `git status *`, `git diff *`, `git log *`, `git show *`, `git branch*`, `git rev-parse *` | `allow` | inspeção sem efeito colateral, evita fadiga de aprovação |
| `shell` `ls *`, `cat *`, `head *`, `tail *`, `wc *`, `grep *`, `rg *`, `find *`, `pwd`, `tree *` | `allow` | leitura de arquivos |
| `shell` `rm *`, `rmdir *`, `mv *`, `truncate *`, `dd *` | `ask` | exclusão/sobrescrita sempre passa pelo usuário |
| `shell` `git commit*`, `git push*`, `git merge*`, `git rebase*`, `git reset *`, `git clean*`, `git tag*`, `git restore *`, `git checkout -- *`, `gh*` | `deny` | o usuário revisa antes do commit; proibido descartar alterações |
| `shell` `*` (qualquer outro comando) | `ask` | inclui instalação de dependência, build, scripts |
| `edit` | `allow` | edição dentro do diretório validado é o trabalho do agente; revisão acontece depois |
| `read` `.env*`, `*id_rsa*`, `*.pem`, `*.pfx` | `deny` | segredo nunca entra no contexto |
| `webfetch`, `websearch` | `ask` | saída de rede não é determinística |

Antes de fechar esta tabela na implementação (T4), confirmar contra o servidor:

- os nomes de ação (`shell`, `edit`, `read`, `external_directory`, `webfetch`, `websearch`) e a precedência entre regra específica e `shell *`;
- a semântica de curinga (valor inteiro, `*` no fim também casa o comando sem argumento);
- o comportamento de comandos compostos (`cd x && rm -y`) — o scanner pode gerar várias verificações.

Nenhuma dessas confirmações depende do modelo: é o servidor que aplica.

### 9.3 Processo (PMW)

- Nenhum `git` de escrita é executado pelo Meu Querido: só `rev-parse`, `status`, `diff --stat`.
- Commit, push, merge, reset e deploy exigem ação humana fora do Meu Querido.
- Antes de iniciar uma execução, `git status --porcelain` é lido e exibido: se o repositório já estava sujo, a UI avisa e o preâmbulo informa o agente para não reverter nada.
- Proibido enviar ao modelo: token, chave SSH, conteúdo de `.env`, conteúdo de `appsettings.*`, corpo de DTO cru.
- Toda operação relevante é registrada (projeto, diretório, ação, resultado), sem segredos.

### 9.4 Dívida pré-existente (fora do escopo, registrada)

1. `GET /api/pastas` devolve `GitHubToken` ao navegador (`backend/src/Services/PastaService.cs:221`, campo em `PastaResponseDTO`). Não é o alvo do beta e **não deve ser corrigido junto** (mantém o diff pequeno), mas é motivo definitivo para nunca repassar DTO do PMW ao modelo.
2. `PastaJsonService` lê `pastas.json` sem `PropertyNameCaseInsensitive` (`Services/PastaJsonService.cs:66`) e `DeleteAsync` faz `p.Diretorio.Equals(...)` sem proteção (`:95`). Um `pastas.json` com campo ausente/em camelCase derruba a listagem com `NullReferenceException`. Afeta qualquer consumidor de `PastaService.ObterTodas()`, inclusive o Meu Querido. Corrigir em tarefa própria, fora deste beta.

## 10. Fluxo ponta a ponta

1. Usuário abre `/meu-querido` → `GET /api/meu-querido/status`.
2. Indisponível → tela explica o motivo e oferece "iniciar OpenCode". Chat desabilitado.
3. `GET /api/meu-querido/projetos` → seleção de projeto conhecido (pastas com repositório cadastrado).
4. Seleção → backend valida diretório e lê `git status`. Alterações pendentes aparecem antes de qualquer execução.
5. `POST /api/meu-querido/mensagens` com `{ projetoId, texto }`.
6. Backend reusa a sessão do mapa ou cria (`location.directory` + `permissions`).
7. Primeira mensagem da sessão leva o preâmbulo de contexto.
8. `POST /prompt`.
9. UI consulta `GET /api/meu-querido/conversa/{projetoId}` a cada ~1s enquanto `executando`.
10. Permissão pendente → estado `aguardandoPermissao` → usuário aprova/rejeita → `reply`.
11. Termina (`idle` com `outcome`) → estado `ocioso` e resumo final.
12. `GET /api/meu-querido/revisao/{projetoId}` → branch, arquivos, diffstat, "nenhum commit realizado".
13. Usuário continua a conversa na mesma sessão ou abre o projeto na IDE (`ComandoService.AbrirPastaIDE`).

### Estados da conversa

```
indisponivel → ocioso → executando → ocioso
                    ↘ aguardandoPermissao ↗
                    ↘ erro
```

## 11. Escopo do Beta 0000001

**Entra**

- Chat integrado ao PMW com seleção de projeto.
- Banco SQLite próprio do Meu Querido (`sessoes`, `operacoes`, `schema_version`), já preparado para memória.
- Sessão OpenCode criada/reaproveitada programaticamente, com permissões restritivas.
- Instrução natural → execução real no diretório real → progresso e resultado na tela.
- Aprovação/rejeição de comando pedido pelo agente.
- Interromper execução.
- Revisão das alterações com aviso explícito de que nada foi commitado.
- Diagnóstico de falha legível (serviço off, timeout, projeto inválido, sessão perdida).
- Indicador de disponibilidade no PMW (nav desabilitada quando indisponível).
- Exibição do agente e do modelo que a sessão está usando (vem da própria sessão, não é presumido).

**Fica fora**

| Item | Motivo |
|-|-|
| Voz | não valida a hipótese |
| Memória de longo prazo, "onde paramos ontem", "prioridades da semana" | o PMW só tem `UrlBaseGestorTarefas` como link; não há integração com gestor de tarefas |
| Clone e criação de branch por comando natural | já existe fluxo próprio (`CloneService`); envolve escrita no disco e remoto |
| Commit, push, merge, deploy pelo assistente | proibido no beta; a demonstração exige que nada seja commitado |
| Execução autônoma, planejamento, personalidade | não necessário |
| MCP | sem necessidade comprovada (seção 6) |
| Multi-projeto simultâneo, subagentes, worktrees | complexidade sem retorno nesta etapa |
| Windows | fora do escopo desta versão (v0.1 é Linux) |

## 12. Estratégia de testes

Proporcional ao beta e em ciclo separado da implementação (regra do projeto: implementação e testes não se misturam).

- **Unitários (xUnit, backend):** validador de contenção de caminho; parsing do registro do serviço; montagem do header Basic e do ruleset; montagem do preâmbulo sem campos sensíveis; normalização de mensagens do OpenCode; parsing de `git status --porcelain`.
- **Sem rede nos testes:** cliente do OpenCode atrás de interface, com fixtures de JSON reais capturados do `/openapi.json` e das respostas observadas.
- **Manual roteirizado:** a demonstração da seção 13, executada do começo ao fim.
- **Frontend:** sem teste unitário (padrão do projeto); validação por `pnpm run type-check` e uso real.

## 13. Critérios de conclusão do Beta 0000001

Demonstração única, no próprio repositório do PMW rodando em Development (`Banco_Dev`, sem tocar produção).

| # | Critério | Como provar |
|-|-|-|
| 1 | Interface envia e recebe mensagens | duas mensagens seguidas, respostas coerentes, histórico visível |
| 2 | Core consulta dados reais do projeto | tela mostra caminho e branch vindos do PMW, conferindo com `git -C <dir> branch --show-current` |
| 3 | Sessão do OpenCode acionada programaticamente | sessão criada pelo Meu Querido aparece em `GET /api/session?directory=<dir>` |
| 4 | Execução real no repositório correto | instrução cria um arquivo delimitado em `docs/`; arquivo existe no PMW e o `git status` mostra **apenas** ele |
| 5 | Andamento e resultado na interface | durante a execução a UI mostra ferramenta/estado; ao fim, resumo |
| 6 | Falha comunicada sem estado inconsistente | parar o serviço do OpenCode no meio → UI mostra `indisponivel`; religar e continuar sem reiniciar o PMW |
| 7 | Alterações visíveis para revisão | painel lista o arquivo criado + diffstat |
| 8 | Nenhum commit sem autorização | `git log --oneline -1` idêntico antes e depois; tentativa de `git commit` pelo agente é bloqueada (`deny`) |
| 9 | Permissão sob controle do usuário | comando de exclusão/shell pedido pelo agente aparece para aprovação; rejeitar impede a execução |
| 10 | Interrupção | botão parar encerra a execução e a sessão volta a `ocioso` |
| 11 | Produção intacta | `Banco/` de produção com timestamps e conteúdo inalterados durante toda a demonstração |

## 14. Riscos

| Risco | Severidade | Mitigação |
|-|-|-|
| API do OpenCode é experimental (`0.0.1`) e pode mudar | alto | cliente isolado, contrato mínimo, checagem de versão em `/api/info`, erro explícito em incompatibilidade |
| Duas autoridades sobre o repositório (PMW executa comandos, agente executa shell) | alto | shell `ask` + `deny` em git destrutivo; PMW não faz git de escrita |
| Consumo da assinatura do usuário por mensagem | médio | preâmbulo curto, sem reenvio de histórico, custo/tokens da sessão visíveis |
| Fadiga de aprovação de shell | médio | allowlist read-only ampla o suficiente para inspeção |
| Serviço do OpenCode compartilhado com a TUI | médio | nunca reiniciar automaticamente; interrupção é por sessão |
| Mapa projeto→sessão perdido/corrompido | baixo | reconstrução por `GET /api/session?directory=` |
| Repositório já sujo antes da execução | médio | aviso na UI + preâmbulo; revisão mostra apenas o que mudou |
| Agente editar o working tree do próprio PMW (repositório de teste é o de desenvolvimento) | alto | `git status --porcelain` exibido antes de cada execução; instruções delimitadas a arquivo novo em `docs/`; commit sempre fora do Meu Querido; revisão obrigatória antes de seguir |
| Stack de teste derrubar a instância do usuário (porta 2024/2025) | alto | subir a instância de teste com `ASPNETCORE_URLS` em porta alternativa e `--no-launch-profile`; nunca matar processo por nome |
| Caminho com espaço/acento | baixo | testes do validador; `git -C` com quoting |
| SQLite nativo fora do publish self-contained | médio | validar `dotnet publish -c Release -r linux-x64` e executar o binário gerado antes de fechar o beta |
| Banco travado por conexão não descartada | médio | conexão por operação com `using`, WAL, transação curta |
| `external_directory` não é contido | alto | Regra `deny` explícita na criação da sessão + validação de caminho no PMW; nunca depender do default do OpenCode (seção 9.2) |
| Modelo/agente vem do `opencode.json` do projeto (no PMW: agente `fullstack-dev`, model `deepseek/deepseek-flash`) | baixo | exibir na UI qual agente/modelo a sessão usa, em vez de presumir; repositório sem `opencode.json` cai no config global |

## 15. Limitações desta investigação

- Tudo foi validado em **Linux**. Descoberta do serviço no Windows (`opencode debug paths state`) não foi verificada e fica fora da v0.1.
- Nenhuma sessão foi criada e nenhum prompt foi enviado ao OpenCode: nenhum token consumido, nenhum estado alterado.
- Os efeitos reais de `permissions` foram lidos na documentação V2 e no `opencode.json` do projeto, não exercitados.
- O formato dos eventos SSE além de `server.connected` foi inferido do binário e da documentação, não observado em execução.

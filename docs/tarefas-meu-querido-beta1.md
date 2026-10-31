# Tarefas — Meu Querido Beta 0000001 (v0.1, Linux)

> Executável. Um item por vez, cada um verificável sozinho.
> Arquitetura, decisões e justificativas: `docs/plano-meu-querido-beta1.md`.
> Nada aqui está implementado (exceto o que estiver marcado).

## Como usar

1. Decisões A–E aprovadas: A .NET, B polling, C `ask`/`deny`, D SQLite, E PMW em Development.
2. Executar na ordem. T1 e T2 podem andar em paralelo; T7 depende de T4–T6.
3. Marcar `[x]` só com o critério de aceitação comprovado.
4. Não commitar nada neste repositório: o usuário revisa e commita pessoalmente.

## Definição de pronto (vale para toda tarefa)

- `dotnet build` sem erro e sem novo warning relevante (backend).
- `pnpm run type-check` e `pnpm run format` (frontend).
- Code style do projeto respeitado (sem comentário no código, early return, sem if/else, camadas).
- Nenhum segredo em código, log ou resposta.
- Nenhum teste unitário criado nesta fase (ciclo separado, via skill `pipeline`).

---

## T0 — Pré-requisitos e ambiente de teste

**Objetivo:** ambiente de demonstração pronto, sem risco para os dados de produção.

- [ ] Confirmar que o backend roda em `Development` e que o banco em uso é `Banco_Dev` (`GET /api/configuracoes/caminho-banco`).
- [ ] Confirmar `DiretorioRaiz` preenchido na configuração de dev e que o repositório `ProjectManagerWeb` aparece em `GET /api/pastas` com `repositorioId` válido.
- [ ] Registrar o estado inicial do working tree (`git status --porcelain`) e do último commit (`git log --oneline -1`) para comparar no fim.
- [ ] Se precisar subir instância de teste: porta alternativa (`ASPNETCORE_URLS=http://localhost:2034` + `--no-launch-profile`), sem derrubar a instância do usuário na 2024/2025. Encerrar pelo PID que escuta a porta (`ss -ltnp | grep :2034`): o `dotnet run` deixa o app como processo filho, então matar só o PID do `run` não libera a porta.

**Critério de aceitação:** a pasta `PMWMQ_Meu_Querido` aparece como projeto válido (com repositório) e o diretório de trabalho resolvido bate com `git -C <dir> rev-parse --show-toplevel`.

**Não fazer:** apagar/limpar `Banco/` de produção; iniciar PMW em `Production` para testar; matar processo por nome (`pkill`, `killall`).

---

## T1 — Cliente do OpenCode e diagnóstico

**Objetivo:** o PMW sabe se o OpenCode está disponível e com qual versão, sem quebrar quando não está.

**Arquivos previstos**

- `backend/src/Services/MeuQuerido/IOpenCodeClient.cs`
- `backend/src/Services/MeuQuerido/OpenCodeClient.cs`
- `backend/src/Services/MeuQuerido/OpenCodeDescoberta.cs` (binário, estado, registro)
- `backend/src/DTOs/StatusOpenCodeDTO.cs`
- `backend/src/Controllers/MeuQueridoController.cs` (só `status` nesta tarefa)
- `backend/Program.cs` (registro no DI)

**Dependências:** nenhuma.

**Passos**

1. Revisar `.opencode/referencias/backend-boilerplates.md` antes de criar arquivo novo.
2. Descoberta: binário no `PATH` → fallback `~/.opencode/bin/opencode`; estado via `opencode debug paths state`; ler `service.json` (aceitar `service-*.json`).
3. Auth: HTTP Basic, usuário `opencode`, senha do registro. Nunca logar a senha.
4. Saúde: `GET /api/info` com timeout de 3s; exigir versão iniciando em `2.`.
5. Mapear falhas em `{ disponivel: false, motivo }`: binário ausente, registro ausente, sem permissão de leitura, serviço mudo, 401, versão incompatível.

**Critérios de aceitação**

- [x] `GET /api/meu-querido/status` → 200 `{ disponivel: true, versao: "2.2.5" }` nesta máquina.
- [ ] Parar o serviço (`opencode service stop`) → 200 `{ disponivel: false, motivo }` legível, sem exceção no log. *(não executado: derrubaria a sessão do próprio agente; validar manualmente)*
- [ ] Subir de novo (`opencode service start`) → volta a `disponivel: true` sem reiniciar o PMW. *(depende do item anterior)*
- [x] Senha ausente no log e na resposta (conferir o log da aplicação).
- [x] Caminho negativo sem binário → 200 `{ disponivel: false, motivo: "Binário 'opencode' não encontrado..." }` (validado com `PATH`/`HOME` isolados).

**Evidências da implementação**

```
GET /api/meu-querido/status (porta 2034)
{"disponivel":true,"versao":"2.2.5","url":"http://127.0.0.1:49374","motivo":null}

GET /api/meu-querido/status (porta 2035, PATH=/usr/bin:/bin, HOME=/tmp/opencode/fakehome)
{"disponivel":false,"versao":null,"url":null,"motivo":"Binário 'opencode' não encontrado no PATH nem em /tmp/opencode/fakehome/.opencode/bin/opencode"}
```

Arquivos entregues: `Services/MeuQuerido/{OpenCodeDescoberta,IOpenCodeClient,OpenCodeClient}.cs`, `DTOs/StatusOpenCodeDTO.cs`, `Controllers/MeuQueridoController.cs`, registro no `Program.cs`.

**Validação manual (comandos previstos)**

```bash
# senha nunca impressa
P=$(python3 -c "import json,os;print(json.load(open(os.path.expanduser('~/.local/state/opencode/service.json')))['password'])")
curl -s -u "opencode:$P" http://127.0.0.1:49374/api/info
curl -s http://localhost:2034/api/meu-querido/status   # instância de teste, porta alternativa
```

**Não fazer:** subir um segundo servidor OpenCode; reiniciar o serviço automaticamente; assumir caminho fixo de estado.

**Riscos:** nome do arquivo de registro varia por canal; `opencode` pode não estar no `PATH` do serviço systemd do PMW (fallback + motivo claro).

---

## T2 — Projetos locais válidos

**Objetivo:** `projetoId` → diretório absoluto confiável, com metadados enxutos.

**Arquivos previstos**

- `backend/src/Services/MeuQuerido/ProjetoLocalService.cs`
- `backend/src/DTOs/ProjetoLocalDTO.cs`
- `backend/src/Controllers/MeuQueridoController.cs` (`GET projetos`)
- `backend/src/Utils/` (validador de contenção de caminho, se necessário reutilizável)

**Dependências:** nenhuma (usa `PastaService`, `RepositorioJsonService`, `ConfiguracaoService`).

**Passos**

1. Derivar a lista de `PastaService.ObterTodas()`, descartando `RepositorioId = Guid.Empty`.
2. Resolver diretório de trabalho com a mesma regra de `ComandoService.cs:16` (`pasta.Diretorio + repositorio.Nome`).
3. Validar: caminho normalizado, existência, `.git`, contenção em `DiretorioRaiz` ou pasta centralizadora.
4. Branch atual via `IGitCommandRunner` (`git -C <dir> branch --show-current`).
5. `projetoId` estável: usar o identificador da pasta quando existir, com fallback documentado.

**Critérios de aceitação**

- [x] `GET /api/meu-querido/projetos` lista só projetos válidos, com `diretorioTrabalho` e `branchAtual`.
- [x] Resposta não contém `githubToken` nem `caminhoChaveSSH` (conferir o JSON).
- [x] Pasta sem repositório cadastrado não aparece.
- [x] `DiretorioRaiz` vazio → lista vazia, 200, sem exceção.
- [x] Caminho com espaço (`PMWMQ_Meu_Querido` já tem) funciona.
- [x] `projetoId` inexistente → 404 com motivo.

**Evidências da implementação**

Ambiente isolado (`HOME` temporário + `Banco_Dev` próprio), sem tocar nos dados reais:

```
GET /api/meu-querido/projetos
[{"projetoId":"2222...","codigo":"PMWMQ","descricao":"Meu Querido",
  "repositorio":"ProjectManagerWeb",
  "diretorio":"/home/wallace/git/pessoal/PMWMQ_Meu_Querido",
  "diretorioTrabalho":"/home/wallace/git/pessoal/PMWMQ_Meu_Querido/ProjectManagerWeb",
  "branchAtual":"PMWMQ"}]

GET /api/meu-querido/projetos/{id inexistente} → 404 "Projeto não encontrado"
DiretorioRaiz fora da pasta → []
DiretorioRaiz nulo → []
```

Arquivos entregues: `Services/MeuQuerido/ProjetoLocalService.cs`, `Utils/ValidadorCaminho.cs`, `DTOs/ProjetoLocalDTO.cs`, endpoints `projetos` e `projetos/{id}` no `MeuQueridoController`, registro no `Program.cs`.

**Achados operacionais**

- `Banco_Dev/pastas.json` está `[]`: no ambiente de desenvolvimento nenhuma pasta aparece vinculada a repositório, então `projetos` retorna vazio até cadastrar a pasta na tela inicial (pré-requisito da demonstração, ver T0).
- `PastaJsonService` desserializa **sem** `PropertyNameCaseInsensitive` (`Services/PastaJsonService.cs:66`). JSON escrito à mão precisa estar em PascalCase; camelCase resulta em `Diretorio = null` e NRE em `DeleteAsync` (`PastaJsonService.cs:95`). Bug pré-existente, registrado como dívida no plano e não corrigido neste beta.

**Riscos:** pastas antigas em `pastas.json` sem diretório no disco (usar apenas o que existe); symlink fora da raiz (deve ser rejeitado).

---

## T3 — Persistência SQLite

**Objetivo:** o Meu Querido tem banco próprio, com esquema versionado, sem tocar na persistência JSON do PMW.

**Arquivos previstos**

- `backend/src/Services/MeuQuerido/MeuQueridoSqliteService.cs` (conexão, DDL, migrations)
- `backend/src/DTOs/SessaoMeuQueridoDTO.cs`
- `backend/src/DTOs/OperacaoMeuQueridoDTO.cs`
- `backend/ProjectManagerWeb.csproj` (`Microsoft.Data.Sqlite`)
- `backend/Program.cs` (registro no DI)

**Dependências:** nenhuma.

**Passos**

1. Adicionar o pacote `Microsoft.Data.Sqlite` (versão alinhada ao .NET 9).
2. Banco em `Path.Combine(PathHelper.BancoPath, "meuquerido.db")` — herda `Banco_Dev` em Development automaticamente.
3. DDL da v1, idempotente: `sessoes`, `operacoes`, `schema_version` (ver plano, seção 2.2); `PRAGMA journal_mode=WAL`.
4. Versionamento: aplicar DDL pendente na primeira inicialização, registrando em `schema_version`. Sem `dotnet ef`, sem `MigrationService`.
5. API do serviço: `ObterSessaoAsync(projetoId)`, `SalvarSessaoAsync(sessao)`, `RegistrarOperacaoAsync(operacao)`, `ListarOperacoesAsync(projetoId)`.
6. Conexão por operação com `using`; transação curta; nenhum campo sensível.

**Critérios de aceitação**

- [ ] Subir o backend cria `Banco_Dev/meuquerido.db` com as três tabelas.
- [ ] Segunda inicialização não duplica nem falha (idempotente); `schema_version` continua com uma linha.
- [ ] Reiniciar o backend traz a sessão salva (teste via endpoint temporário ou T4).
- [ ] `PRAGMA journal_mode` retorna `wal`.
- [ ] Nenhuma coluna/registro com token, chave SSH ou conteúdo de `.env`.
- [ ] `dotnet publish -c Release -r linux-x64` gera binário que abre o banco (SQLite nativo embarcado).

**Validação manual**

```bash
sqlite3 ~/.config/PMW/Banco_Dev/meuquerido.db ".tables"
sqlite3 ~/.config/PMW/Banco_Dev/meuquerido.db "select * from schema_version;"
```

**Não fazer:** gravar histórico de mensagens (a fonte é o OpenCode); reaproveitar `MigrationService` de JSON; deixar conexão aberta em campo estático.

**Riscos:** caminho do banco em produção ser somente leitura no IIS (`%APPDATA%` já é gravável pelo PMW, então baixo); publish self-contained sem a lib nativa.

---

## T4 — Sessão e envio de mensagem

**Objetivo:** mensagem do usuário chega ao OpenCode na sessão certa do projeto, com contexto mínimo.

**Arquivos previstos**

- `backend/src/Services/MeuQuerido/MeuQueridoService.cs`
- `backend/src/DTOs/{EnviarMensagemRequestDTO,ConversaResponseDTO,MensagemResponseDTO}.cs`
- `backend/src/Controllers/MeuQueridoController.cs` (`mensagens`, `conversa/{projetoId}`, `interromper`)
- `backend/src/Services/MeuQuerido/{IOpenCodeClient,OpenCodeClient}.cs` (create/prompt/message/session/interrupt)

**Dependências:** T1, T2, T3.

**Passos**

1. Sessão: consultar `sessoes` no SQLite; se ausente, `POST /api/session` com `location.directory` e o ruleset da T5 (ruleset entra junto, mesmo que a UI de aprovação venha depois) e salvar o registro.
2. Preâmbulo na primeira mensagem da sessão: projeto, repositório, diretório, branch, arquivos já modificados, proibições. Sem token, sem chave, sem conteúdo de `.env`.
3. Enviar `POST /prompt` e ignorar o retorno para fins de estado (retorno é inbox, não conclusão).
4. Montar snapshot: mensagens normalizadas + estado derivado de `GET /api/session/{id}` (`time.busy`/`time.idle`/`outcome`) + agente/modelo da sessão.
5. Registrar em `operacoes` cada envio e cada término (tipo, alvo, resultado), sem conteúdo sensível.
6. Timeout de execução de 10 min → `interrupt` + erro na conversa.
7. Sessão inexistente (404) → recriar, atualizar o banco e avisar.

**Critérios de aceitação**

- [ ] Primeira mensagem cria sessão com `location.directory` igual ao diretório validado (conferir em `GET /api/session?directory=<dir>`).
- [ ] Segunda mensagem reusa a mesma sessão; o registro em `sessoes` não duplica.
- [ ] `GET /api/meu-querido/conversa/{projetoId}` devolve mensagens com papel e texto, estado correto durante e após execução, agente e modelo.
- [ ] `POST /interromper` encerra e volta a `ocioso`.
- [ ] Reiniciar o PMW mantém o vínculo com a sessão (lido do SQLite).
- [ ] Nenhum campo sensível no preâmbulo (teste de montagem no ciclo de testes).

**Riscos:** tratar `prompt` como síncrono é o erro mais provável — o estado tem que vir da sessão/mensagens.

---

## T5 — Permissões sob controle do usuário

**Objetivo:** o agente só faz o que o usuário autoriza.

**Arquivos previstos**

- `backend/src/Services/MeuQuerido/OpenCodePermissoes.cs` (ruleset padrão + leitura de pendências)
- `backend/src/DTOs/{PermissaoResponseDTO,ResponderPermissaoRequestDTO}.cs`
- `backend/src/Controllers/MeuQueridoController.cs` (`permissoes/responder`)
- `backend/src/Services/MeuQuerido/MeuQueridoService.cs` (estado `aguardandoPermissao`)

**Dependências:** T4.

**Passos**

1. Confirmar os nomes de ação, a precedência (regra específica × `shell *`) e a semântica de curinga contra o servidor (documentação V2 + `GET /api/config` + `opencode.json` do projeto).
2. Ruleset ao criar a sessão, conforme seção 9.2 do plano: `external_directory` deny; allowlist read-only (`git status/diff/log/show/branch/rev-parse`, `ls`, `cat`, `grep`, `rg`, `find`, `head`, `tail`, `wc`, `pwd`); `ask` em `rm`/`rmdir`/`mv`/`truncate`/`dd`; `ask` no restante de `shell`; `deny` em `git commit/push/merge/rebase/reset/clean/tag/restore`, `git checkout --`, `gh`; `edit` allow; `read` deny em `.env*`/`id_rsa*`/`*.pem`/`*.pfx`; `webfetch`/`websearch` ask.
3. Ler pendências (`GET /api/permission/request`) e expor no snapshot da conversa.
4. Responder com `once`/`reject`; nunca automático, nunca "lembrar" sem ação do usuário.
5. Sem resposta em 5 min → `interrupt` + erro na conversa.

**Critérios de aceitação**

- [ ] Sessão criada carrega o ruleset (conferir em `GET /api/session/{id}` → `permissions`).
- [ ] Comando de exclusão (`rm`) gera `aguardandoPermissao` com o comando legível na conversa.
- [ ] Aprovar executa; rejeitar impede (evidência no histórico da sessão).
- [ ] Comando de leitura da allowlist roda **sem** pedir aprovação.
- [ ] Instrução para `git commit` é bloqueada por `deny`, sem depender do modelo.
- [ ] `external_directory` deny impede escrita fora do diretório do projeto (teste com caminho externo explícito).

**Riscos:** curinga do OpenCode é por valor inteiro, não regex — validar padrões antes de confiar; fadiga de aprovação; allowlist curta demais gera ruído e longa demais perde controle.

---

## T6 — Revisão das alterações

**Objetivo:** provar que nada foi commitado e mostrar o que mudou.

**Arquivos previstos**

- `backend/src/Services/MeuQuerido/RevisaoService.cs`
- `backend/src/DTOs/RevisaoResponseDTO.cs`
- `backend/src/Controllers/MeuQueridoController.cs` (`revisao/{projetoId}`)

**Dependências:** T2.

**Passos**

1. `git -C <dir> branch --show-current`
2. `git -C <dir> status --porcelain`
3. `git -C <dir> diff --stat`
4. `git -C <dir> log --oneline -1` (para comprovar ausência de commit)

**Critérios de aceitação**

- [ ] Resposta traz branch, arquivos (incluindo untracked) e resumo.
- [ ] `commitRealizado: false` e último commit igual ao de antes da execução.
- [ ] Nenhum comando de escrita é executado (conferir no log de comandos).
- [ ] Funciona com repositório sujo previamente.

**Riscos:** `GitCommandRunner` não tem `WorkingDirectory` → usar `git -C`; quoting com espaços.

---

## T7 — Interface do Meu Querido

**Objetivo:** tela mínima que cobre o fluxo, sem lógica no template.

**Arquivos previstos**

- `frontend/src/router/index.ts` (rota `/meu-querido`)
- `frontend/src/App.vue` (item de navegação, desabilitado quando indisponível)
- `frontend/src/views/MeuQueridoView.vue`
- `frontend/src/components/meuQuerido/{SelecaoProjeto,JanelaConversa,MensagemConversa,AprovacaoComando,PainelRevisao}.vue`
- `frontend/src/stores/meuQuerido.ts`
- `frontend/src/services/meuQueridoService.ts`
- `frontend/src/models/{ProjetoLocalModel,ConversaModel,RevisaoModel}.ts`
- `frontend/src/types/index.ts` (interfaces)

**Dependências:** T1, T2, T4, T5, T6.

**Passos**

1. Revisar `.opencode/referencias/frontend-boilerplates.md` e uma store existente (`monitoramento.ts`) antes de criar.
2. Store Options API: estado da conversa, projeto selecionado, polling controlado (só enquanto `executando`), `finalizando` no `finally`.
3. Componente ≤ 200 linhas; condicionais e classes via `computed`; sem `console.log` (usar o snackbar existente).
4. Polling a cada ~1s, interrompido ao desmontar o componente.

**Critérios de aceitação**

- [ ] Fluxo completo executável pela tela: status → selecionar projeto → enviar → ver progresso → aprovar → ver revisão.
- [ ] `pnpm run type-check` limpo; `pnpm run format` aplicado.
- [ ] Polling para quando ocioso e ao sair da tela (conferir Network).
- [ ] Erro de backend aparece em snackbar, não em `console`.
- [ ] Cabeçalho da conversa mostra projeto, diretório, agente e modelo vindos do backend.
- [ ] Nenhuma requisição do navegador ao OpenCode.

---

## T8 — Amarras finais de segurança e diagnóstico

**Objetivo:** fechar entradas e vazamentos.

**Arquivos previstos:** controller, DTOs, `MeuQueridoService`, `frontend/src/App.vue` (gating), `backend/src/Controllers/VersaoController.cs` (se optar por feature flag).

**Dependências:** T7.

**Passos**

1. Rejeitar payloads que tentem mandar caminho (`diretorio` no corpo é ignorado/rejeitado).
2. Normalização de mensagens: só texto, nome de ferramenta, status e resumo.
3. Registrar operação (projeto, diretório, ação, resultado) no SQLite, sem segredos.
4. Gating da navegação/rota pelo `status`.

**Critérios de aceitação**

- [ ] Tentativa de envio com `diretorio` arbitrário retorna 400 e não afeta nada.
- [ ] Nenhuma resposta da API contém token, chave SSH ou conteúdo de `.env`.
- [ ] `grep -ri "githubToken\|caminhoChaveSSH" backend/src/Services/MeuQuerido backend/src/DTOs/*MeuQuerido*` sem ocorrência indevida.
- [ ] Rota inacessível (ou desabilitada) quando o OpenCode está indisponível.

---

## T9 — Documentação e encerramento

**Objetivo:** deixar o fluxo descobrível para o próximo agente.

**Dependências:** T7.

- [ ] `.opencode/flows/meu-querido.md` com o fluxo real implementado (front → controller → service → OpenCode).
- [ ] `.opencode/ecosystem-pmw.md`: linha no índice apontando o novo fluxo, uma linha no domínio e o banco SQLite em "Persistência".
- [ ] `docs/plano-meu-querido-beta1.md`: marcar como implementado e registrar desvios do plano.
- [ ] Ciclo de testes (skill `pipeline`/`testador`) depois da entrega manual.
- [ ] Roteiro da demonstração executado e registrado no ticket/PR.

---

## Demonstração de aceite (resumo operacional)

Executada no próprio PMW em Development.

1. Abrir `/meu-querido` com o OpenCode rodando.
2. Selecionar o projeto `PMWMQ_Meu_Querido / ProjectManagerWeb`.
3. "Liste os arquivos da pasta docs" → conferir resposta (leitura, sem aprovação).
4. "Crie o arquivo `docs/beta1-check.md` com o texto 'beta 1 ok'" → aprovar se houver pedido de shell.
5. Conferir painel de revisão: `docs/beta1-check.md` listado, diffstat, nenhum commit.
6. `git status --porcelain` mostra o arquivo novo; `git log --oneline -1` igual ao registrado no T0.
7. "Agora faça o commit dessas alterações" → bloqueado por `deny`, com motivo visível.
8. "Apague o arquivo `docs/beta1-check.md`" → deve aparecer como permissão pendente; rejeitar mantém o arquivo.
9. Reiniciar o backend → conversa do mesmo projeto volta pela sessão lida do SQLite.
10. Parar o serviço do OpenCode no meio de uma execução → UI mostra indisponível; religar e continuar sem reiniciar o PMW.
11. Conferir que o banco de produção (`Banco/`) não mudou e que a instância do usuário na 2024/2025 continuou de pé.

O arquivo criado no passo 4 fica para revisão do usuário — remoção é decisão dele, fora do Meu Querido.

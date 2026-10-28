# Plano — Registro de uso de recursos da máquina

Status: **etapas 1 a 5 implementadas** (branch `PMW29`) — registro de dados e relatório HTML funcionando e medidos.
Objetivo: provar com dados que o notebook corporativo satura a CPU e que isso prejudica o trabalho.

## Contexto de negócio

Notebook da empresa (i5-1135G7) fica longos períodos em 100% de CPU. Pedido de troca negado sem evidência.
Meta: relatório HTML autocontido, enviável por e-mail, mostrando episódios de saturação, quem consome, por quanto tempo — e quanto o próprio PMW custou.

Restrição dura: **não pode pesar a máquina**. Se o custo de registrar for relevante, o recurso não se paga.

## Decisões fechadas

| # | Decisão | Valor |
|---|---|---|
| 1 | Quando gravar | Só com o painel `/monitoramento` aberto **e** `ativo` marcado na aba Monitoramento |
| 2 | Gatilho | Só CPU, `cpu >= limiar` (igual ou maior registra) |
| 3 | Abaixo do limiar | Não registra nada. Sem baseline |
| 4 | Intervalo entre registros | Configurável, padrão **5s** (continua acima do limiar ⇒ registra de novo) |
| 5 | Limiar padrão | **80%** |
| 6 | Arquivo | **1 arquivo por dia**, uma linha JSON por amostra (`uso-YYYY-MM-DD.jsonl`) |
| 7 | Escrita | Buffer em memória, gravação em bloco — sem abrir/fechar arquivo a cada amostra |
| 8 | Retenção | **30 dias** (apaga os mais antigos na virada do dia) |
| 9 | Relatório | **Manual**, só ao clicar em "Gerar relatório" na aba Monitoramento |
| 10 | Onde configura | Nova aba **Monitoramento** em `ConfiguracaoView.vue` |
| 11 | Destino do HTML | Pasta separada (`BancoPath/uso/relatorios/`) e **abre automaticamente** ao gerar |
| 12 | Flush do buffer | **60s** (perda máxima aceita: 1 minuto) |
| 13 | Foco | Performance acima de tudo: a feature não pode tornar o PMW mais lento |

## O que o PMW já tem (não reinventar)

| Peça | Arquivo | Estado |
|---|---|---|
| Coletores por plataforma | `backend/src/Services/Monitoramento/Coletores/` | CPU, RAM, disco, I/O disco, rede, temperatura, swap |
| Snapshot | `backend/src/DTOs/MonitoramentoSnapshotDTO.cs` | record com campos opcionais |
| Ciclo de coleta | `MonitoramentoService.cs` | `PeriodicTimer` 1s, **só com cliente WebSocket conectado** |
| Top processos | `ProcessosService` + `Windows/LinuxProcessosColetor` | sob demanda (modal do painel) |
| Persistência | `PathHelper.BancoPath` (`%APPDATA%\PMW\Banco` / `~/.config/PMW/Banco`) | `Banco_Dev` em Development |
| Config | `ConfiguracaoService` + `ConfiguracaoRequestDTO` → `Configuracao.json` | Singleton, `SemaphoreSlim` |
| UI de config | `frontend/src/views/ConfiguracaoView.vue` | abas Geral / Pasta Centralizadora / CLI de IA |

## Achados técnicos

### 1. Gravar dentro do ciclo do painel (resolvido pela decisão #1)

`MonitoramentoService` (Singleton) só coleta com ≥1 socket: `AdicionarConexaoAsync` → `IniciarColeta()` (linha 61), `RemoverConexaoAsync` → `PararLoopAsync()` (linha 82). O frontend fecha o socket quando `MonitoramentoView` desmonta (`stores/monitoramento.ts:48`).

A gravação entra em `LoopColetaAsync` (linha 128), que **já entrega o snapshot completo por segundo**. Consequências:
- Não existe serviço próprio de coleta → some a armadilha do delta de CPU misturado entre instâncias (keyed DI desnecessário).
- Não existe `Task.Delay(500)` nem segunda varredura de processos → some a armadilha do custo do `ColetarTopAsync`.
- Lacuna no registro = **painel fechado**, nunca "CPU normal". O relatório precisa dizer isso.

### 2. Enumeração de processos precisa de amostragem própria

`WindowsProcessosColetor.ObterAmostraCpu()`/`LinuxProcessosColetor.ObterAmostraCpu()` fazem uma varredura da tabela de processos; `ColetarTopAsync` faz **duas** com 500ms entre elas — caro e impreciso para 30s de janela.

Solução: expor a varredura única em `IProcessosColetor` e o gravador calcular o delta entre duas amostras suas (janela real de 30s, sem sleep). O cálculo de percentual hoje está duplicado em Windows e Linux — extrair para helper único.

### 3. WMI é o único pico de custo real

`WindowsCpuRamColetor.ConsultarValorUnico` cria `ManagementObjectSearcher`: temperatura ACPI (TTL 30s), `MaxClockSpeed` (1x), `CurrentClockSpeed` fallback (30s), `% Processor Performance` (a cada amostra). 20-200ms por consulta.
**Nenhuma coleta nova é adicionada** — o gravador consome o snapshot que o painel já produziu. O custo marginal do registro é serialização + append.

### 4. No Windows o PMW não inicia sozinho

`infra/pmw.ps1:74` sobe `ProjectManagerWeb.exe` com `Start-Process`. Não há `schtasks` nem atalho em `shell:startup`.
**Sem PMW aberto não há registro** — e agora, sem painel aberto, também não. Autostart deixa de fazer diferença para esta feature.

## Custo estimado

| Item | Custo |
|---|---|
| `GetSystemTimes` (CPU) | ~1-2 µs |
| `GlobalMemoryStatusEx` (RAM) | ~1 µs |
| `DriveInfo` (disco) | ~0,1-1 ms |
| `DeviceIoControl` (I/O disco, handles abertos) | ~10-50 µs |
| WMI (TTL 30s no painel) | 20-200 ms por consulta |
| Varredura de processos (1x/30s, fora do loop) | ~10-30 ms |
| Serializar + append de 1 linha | ~0,1 ms |
| Gravação em bloco (1 open/write/close por minuto) | desprezível |

Alvo: **< 0,5% de CPU em regime**, picos curtos de dezenas de ms. Referência: painel aberto no navegador custa mais que isso.

Volume: ~600 B/linha × 5s × 8h ≈ **3,5 MB/dia** (≈105 MB/30 dias, dentro da retenção).
Prova do custo: gravar `coletaMs` (duração da amostra) e `pmwCpu` (CPU% do processo PMW) em cada linha.

## Regras de performance (não negociáveis)

O notebook já satura; a feature nasce com orçamento de custo fechado.

| Regra | Motivo |
|---|---|
| Nenhuma coleta nova | o snapshot do painel já tem tudo; o gravador só consome |
| `Registrar` só faz checagem em memória + `List.Add` sob lock | nada de I/O, serialização ou enumeração dentro do loop de 1s |
| Lock do buffer só cobre operações de memória | enumeração de processos e medição de CPU ficam fora do lock |
| Toda serialização e escrita na task vigia (30s) | loop do painel não espera |
| Varredura de processos 1x/30s e só se há amostra pendente | é o item mais caro do app; abaixo do limiar não enumera nada |
| Config em cache com TTL 30s | zero leitura de `Configuracao.json` por segundo |
| 1 open/append/close por flush (60s) | antivírus e SSD |
| Retenção só na virada do dia | varredura de diretório não pode ser por gravação |
| `pmwCpu` lido só na vigia | 2 leituras de `TotalProcessorTime` por minuto, não por segundo |

Medição embutida em cada linha: `coletaMs` (duração de `coletor.ColetarAsync`) e `pmwCpu` (CPU% do próprio PMW).
Critério de aceite: somando tudo, o PMW não pode gastar mais que **0,5% de CPU** com a gravação ligada.

## Desenho

### Backend

```
UsoRecursosService : IHostedService   → fila + buffer + regra do limiar
UsoJsonService                        → 1 JSONL por dia, append em bloco
RelatorioUsoService                   → gera HTML único
UsoController                         → endpoints REST
```

Fluxo:

```
LoopColetaAsync (1s, só com painel aberto)
  ├── coletor.ColetarAsync              (existente) ← cronometra coletaMs
  ├── broadcast WebSocket               (existente, inalterado)
  └── usoRegistrar.Registrar(snapshot, coletaMs)      [NOVO, não bloqueante]
          ├ config em cache (TTL 30s): ativo? cpu >= limiar?
          ├ throttle: agora - ultimoRegistro >= intervalo
          └ List.Add no buffer em memória (lock de microssegundos)

Task vigia (30s)
  ├── recarrega config do cache (TTL)
  ├── se há pendentes: varredura de processos + delta (fora do lock)
  ├── aplica top 10 na última amostra pendente e pmwCpu em todas
  └── flush quando: 60s desde o último | buffer cheio (300)

PararLoopAsync (painel fechou) e StopAsync (PMW encerrando)
  └── flush final do buffer
```

Regras de integridade do buffer:
- Lock cobre apenas cópia/adição de lista; enumeração de processos e `Process.TotalProcessorTime` acontecem fora dele.
- `FlushAsync` no `PararLoopAsync` (painel fechou) e no `StopAsync` do hosted service.
- Gravação em bloco: `StringBuilder` com todas as linhas → 1 `AppendAllTextAsync` por dia por lote, UTF-8 sem BOM.
- Perda máxima em queda abrupta (kill/energia): até 60s de amostras. Documentado no relatório.

### Config

Novo bloco em `ConfiguracaoRequestDTO` (`backend/src/DTOs/ConfiguracaoRequestDTO.cs`):

```csharp
UsoRecursosDTO? UsoRecursos = default
```

| Campo | Padrão | Efeito |
|---|---|---|
| `Ativo` | `false` | Nada grava até marcar |
| `LimiarCpuPercentual` | `80` | Registra quando `cpu >= limiar` |
| `IntervaloRegistroSegundos` | `5` | Espera mínima entre registros |
| `RetencaoDias` | `30` | Dias mantidos em `BancoPath/uso/` |

`ConfiguracaoService` ganha leitura em cache com TTL de 30s, invalidada no `SalvarConfiguracaoAsync` — hoje `ObterConfiguracaoAsync` lê o arquivo a cada chamada, inviável a 1/s.

### Arquivo

`{BancoPath}/uso/uso-YYYY-MM-DD.jsonl` (data local), uma linha por amostra, camelCase:

```
{"ts":"2026-10-26T14:32:05.835-03:00","cpu":100,"ramPercentual":87.4,"ramUsadaBytes":4100000000,
 "discoLivreBytes":23075008512,"discoAtividadePercentual":98.1,"discoLatenciaLeituraMs":142,
 "cpuFrequenciaMhz":1190,"cpuTemperaturaCelsius":71,"discoTemperaturaCelsius":52,
 "coletaMs":3,"pmwCpu":0.06,
 "processos":[{"nome":"MsMpEng","cpu":42.5},{"nome":"chrome","cpu":31}],
 "maquina":{"sistemaOperacional":"Windows 11 Pro","cpuNome":"11th Gen Intel(R) Core(TM) i5-1135G7",
            "nucleos":8,"ramTotalBytes":17179869184,"discoTotalBytes":512110190592}}
```

- `maquina` gravada na **primeira escrita de um dia que ainda não tem** essa informação no arquivo (checa a 1ª linha ao abrir o dia).
- `processos` (top 10 CPU) só nas linhas coincidentes com a vigia de 30s. Nas demais, ausente.
- Campos nulos são omitidos do JSON (`WhenWritingNull`), não gravados como `null`.
- Bytes em vez de MB/GB: o relatório formata. Evita ambiguidade de arredondamento.

- `processos` (top 10 CPU) só nas linhas de 30 em 30s — a cada 6 registros no intervalo de 5s.
- JSONL e não texto solto: 1 arquivo por dia do mesmo jeito, mas o relatório lê sem parser frágil. Se um dia a leitura manual incomodar, é só extensão/nome.
- Dia sem episódio: **arquivo não existe**. O relatório distingue "sem arquivo" de "arquivo sem linhas".

### Endpoints

```
GET  /api/uso/dias                        → [{ data, tamanhoBytes }]
POST /api/uso/relatorio                   → body { data } → { caminho, tamanhoBytes, registros }
```

O HTML é salvo em `{BancoPath}/uso/relatorios/relatorio-uso-YYYY-MM-DD.html` (do dia escolhido, **sobrescrito** se gerar de novo) e aberto no navegador pelo backend
(`ShellExecute` com `IShellProvider`, mesmo caminho usado para abrir IDE/pasta) imediatamente após salvar.

### Relatório (HTML + CSS + JS puro, arquivo único)

Gerado só sob demanda, sem CDN e sem lib. Dados embutidos no HTML.

Layout final (enxuto, sem enfeite):

1. Título **“Registro de consumo do notebook”**.
2. Duas colunas: **Máquina** reduzida à esquerda (sistema, CPU, núcleos físicos, threads, RAM, disco — só rótulo e dado) e **minitabela do top 10 de apps** à direita, apenas nomes, sem percentual.
3. Dois cards: **registros acima do limiar configurado** e **tempo acima do limiar**.
4. Tabela registro a registro (data/hora, CPU, RAM — percentual e GB na mesma célula, disco livre, atividade, latência, frequência, temperatura). Clique na linha expande todos os campos + top 10 de processos daquele momento.
5. Rodapé: data/hora de geração e o aviso de que lacuna = painel fechado ou PMW parado.

- Cabeçalho da máquina: **sempre** faz 1 coleta sob demanda ao gerar (`IColetorMetricas`) e usa a `maquina` do arquivo apenas como fallback se a coleta falhar. Motivo: arquivos gravados por versões antigas trazem `maquina` incompleta (sem `threads`) e envenenariam o cabeçalho. Custo: uma amostra extra; se o painel estiver aberto, o CPU% dele fica 1 frame levemente impreciso.
- Registros antigos (antes da troca de `ramLivreBytes` por `ramUsadaBytes`): o relatório calcula `usada = ramTotal - ramLivre`, então o histórico continua mostrando GB.
- Time acima do limiar = soma dos deltas entre registros consecutivos, descartando lacunas > 2 min.
- Top de apps agregado por **nome** (não por PID), ordenado pela **soma** das porcentagens registradas em cada amostra — quem apareceu no top 10 acumulou mais CPU no período fica no topo (top 10 vilões), independente de ter sido pico único ou consumo constante.
- Sempre de **um dia**: a aba tem seletor de dia (default = hoje) e o arquivo é `relatorio-uso-YYYY-MM-DD.html` daquele dia. Sem `data` no request, usa o dia atual.

### Frontend

- Nova aba **Monitoramento** em `ConfiguracaoView.vue`: ativar, limiar de CPU %, intervalo entre registros, retenção + botão **Gerar relatório** (salva em `BancoPath/uso/relatorios/` e abre no navegador).
- `usoService.ts` (dias, gerar, download) e acesso via store de configuração existente.
- Camadas: Component → Store → Service → API.

## Estado da implementação

Etapas 1 a 3 **implementadas** (branch `PMW29`). Relatório HTML e aba de listagem ainda não.

### Arquivos

| Arquivo | Estado |
|---|---|
| `backend/src/DTOs/ConfiguracaoRequestDTO.cs` | + `UsoRecursos` e `UsoRecursosDTO` |
| `backend/src/DTOs/UsoAmostraDTO.cs` | novo — linha do JSONL e processo |
| `backend/src/DTOs/AmostraProcessoDTO.cs` | novo — amostra de CPU para delta |
| `backend/src/Services/ConfiguracaoService.cs` | cache de 30s (`ObterConfiguracaoEmCacheAsync`, `ConfiguracaoEmCache`), invalidado ao salvar |
| `backend/src/Services/Monitoramento/Uso/UsoRecursosService.cs` | novo — regra do limiar, buffer, vigia, flush, `pmwCpu` |
| `backend/src/Services/Monitoramento/Uso/UsoJsonService.cs` | novo — JSONL por dia, retenção 30 dias, UTF-8 sem BOM |
| `backend/src/Services/Monitoramento/Coletores/IProcessosColetor.cs` | + `ObterAmostraCpu()` |
| `backend/src/Services/Monitoramento/Coletores/TopProcessosCpu.cs` | novo — delta de CPU compartilhado (Windows, Linux e vigia) |
| `backend/src/Services/Monitoramento/Coletores/{Windows,Linux}ProcessosColetor.cs` | usam o helper e expõem a varredura única |
| `backend/src/Services/Monitoramento/MonitoramentoService.cs` | cronometra `coletaMs`, chama `Registrar`, flush no `PararLoopAsync` |
| `backend/Program.cs` | `UsoJsonService` + `UsoRecursosService` como hosted service |
| `frontend/src/types/index.ts` | `IUsoRecursos` em `IConfiguracao` |
| `frontend/src/models/ConfiguracaoModel.ts` | defaults 80% / 5s / 30 dias / inativo |
| `frontend/src/views/ConfiguracaoView.vue` | aba Monitoramento + computeds numéricas |
| `backend/src/DTOs/MaquinaDTO.cs` | novo — SO/CPU/núcleos/RAM/disco para o cabeçalho |
| `backend/src/DTOs/RelatorioUsoDTO.cs` | novo — request/response do relatório |
| `backend/src/Services/Monitoramento/Uso/RelatorioUsoService.cs` | novo — agregação, episódios, top 10, grava e abre |
| `backend/src/Services/Monitoramento/Uso/RelatorioUsoTemplate.cs` | novo — HTML/CSS/JS puro, arquivo único |
| `backend/src/Services/Monitoramento/Uso/RelatorioUsoDados.cs` | novo — modelo serializado no HTML |
| `backend/src/Controllers/UsoController.cs` | novo — `POST /api/uso/relatorio` |
| `frontend/src/services/usoService.ts` | novo |
| `frontend/src/stores/uso.ts` | novo |

### Medição real (smoke test em Development, banco isolado)

Limiar 1%, intervalo 1s, painel aberto por 74s:

| Métrica | Resultado |
|---|---|
| Linhas gravadas | 73 (1/s, como configurado) |
| `pmwCpu` máximo | **0,06%** — bem abaixo do alvo de 0,5% |
| `coletaMs` máximo | 58 ms (pico do coletor de disco que já existia; não é custo do registro) |
| Tamanho por linha | ~870 bytes |
| Flush periódico | aos 60s, 53 linhas |
| Flush no fechamento do painel | +20 linhas |
| Top 10 processos | preenchido na vigia de 30s, nomes e % corretos |

### Relatório HTML — medido

Arquivo único, sem CDN, dados embutidos, gerado sob demanda pelo botão da aba Monitoramento.

| Cenário | Resultado |
|---|---|
| 420 registros (1 dia) | HTML 113 KB |
| 12.068 registros (2 dias) | HTML 2,7 MB, **geração 258 ms**, abertura + render **223 ms** |
| Tabela | paginada em 100 linhas; 12k registros ⇒ 2.068 omitidos da tabela com aviso explícito (totais e top consideram todos) |
| Expansão de linha | ~100 ms, sem erro de JS |

Limite de 10.000 linhas na tabela (`LimiteRegistrosTabela`) para o HTML não virar anexo impossível de e-mail.

### Defeitos encontrados e corrigidos no teste

1. `File.AppendAllTextAsync` com `Encoding.UTF8` gravava **BOM** no início do arquivo → trocado por `UTF8Encoding(false)`. Sem isso o parser do relatório quebraria na primeira linha.
2. Gate de 30s da amostragem de processos comparado com a vigia de 30s caía no jitter e nunca disparava → removido; a vigia já é o relógio.
3. Primeira amostragem gravava `processos: []` → agora `null` quando não há amostra anterior.
4. Intervalo comparado com precisão de milissegundos contra o tick de 1s descartava registros → tolerância de 250 ms.

## Pendências

| # | Pendência | Observação |
|---|---|---|
| 1 | Autostart no Windows | Perde utilidade: sem painel aberto não grava |
| 2 | Conteúdo visual do HTML | Fica para a etapa posterior: primeiro registro de dados funcionando e medido |

## Dívidas conhecidas (decisão: observar 1 semana antes de mexer)

Levantadas na revisão final da primeira versão. Nenhuma é bug — são lacunas de rastreabilidade e robustez.
Ordem por risco real, não por esforço.

| # | Dívida | Consequência | Correção proposta |
|---|---|---|---|
| 1 | **Limiar não é gravado junto ao dado.** O relatório aplica o limiar *atual* a registros coletados com outro limiar | Card "registros acima de 60%" pode descrever dados gravados quando o limiar era 80%. Inconsistência silenciosa — munição contra o próprio relatório | Gravar `limiar` e `intervalo` no **cabeçalho do dia** (mesma linha que já tem `maquina`) |
| 2 | **Intervalo inferido pelos deltas.** Não é persistido | "Tempo acima do limiar" distorce se o intervalo mudar no meio do dia; não auditável | Mesma correção do item 1 |
| 3 | **Falha de gravação perde o lote.** `RetirarBuffer` esvazia antes do `AppendAllTextAsync` | Erro de disco descarta até 60s de amostras, só com log | Devolver o lote ao buffer no `catch` do `FlushAsync` |
| 4 | **Cobertura depende do painel estar aberto** | Episódio de saturação sem painel aberto não existe no relatório. Se questionarem "e nas outras 6 horas?", não há resposta | Se a evidência for contestada: autostart + gravação independente do painel (custo sobe, mas deixa de ser amostral) |
| 5 | `pmwCpu` é a média do lote aplicada a todas as linhas | Valor por linha é aproximado | Ler `TotalProcessorTime` na linha, se algum dia importar precisão |
| 6 | Retenção roda só na virada do dia | PMW parado por dias mantém arquivos além dos 30 | Rodar também no `StartAsync` |
| 7 | JSONL cresce linear; relatório relê tudo a cada geração | 0,26 s para 12k registros; ~3 s para 30 dias a 5 s | SQLite (abaixo) |
| 8 | Sem filtro de data na UI | Botão gera todo o histórico | Backend já aceita `dataInicio`/`dataFim`; falta só o seletor |
| 9 | Fallback de máquina usa o coletor compartilhado | Se o painel estiver aberto ao gerar o relatório, 1 frame de CPU% fica impreciso | Coletor dedicado (keyed DI) |

## Próxima iteração — após 1 semana de dados reais

Decisão registrada: rodar uma semana com o JSONL, olhar o volume e a utilidade do relatório **antes** de trocar a persistência.

Se a semana confirmar o valor da feature, migrar para **SQLite** (`BancoPath/uso.db`):

- Tabela `amostras` com `ts` (índice), `cpu`, `ramPercentual`, `ramUsadaBytes`, `discoLivreBytes`, `discoAtividade`, `discoLatencia`, `freqMhz`, `tempCpu`, `tempDisco`, `coletaMs`, `pmwCpu`.
- Tabela `processos` (`amostraId`, `nome`, `cpu`) ou coluna JSON — decidir pelo volume.
- Tabela `dias` com `data`, `limiar`, `intervalo`, `maquina` — resolve os itens 1 e 2 das dívidas.
- Migração única dos `.jsonl` existentes.
- Ganhos: consulta por período sem varrer arquivo, relatório instantâneo com 30 dias, retenção por `DELETE` em vez de varrer diretório.
- Custo: dependência nova (`Microsoft.Data.Sqlite`), escrita transacional por lote (não por amostra) e um serviço de migração.

## Critérios de aceite

- Painel aberto + ativo + CPU abaixo do limiar ⇒ nada é gravado.
- CPU igual ou acima do limiar ⇒ 1 linha a cada `intervalo`, no arquivo do dia.
- Painel fechado ⇒ nenhuma linha nova; dados em buffer já foram para o arquivo.
- Passar de 30 dias ⇒ dias mais antigos apagados.
- Relatório abre offline em qualquer navegador, cabe em anexo de e-mail e abre sozinho após ser gerado.
- Custo do PMW com a gravação ligada < 0,5% de CPU.
- Nenhuma alteração de comportamento no painel `/monitoramento` existente.

## Fora de escopo (YAGNI)

- Coleta de GPU, rede por processo, bateria/energia.
- Envio automático de dados para fora da máquina.
- Dashboard novo no SPA (o entregável é o HTML).
- Compactação dos dias antigos.
- Agendamento do relatório.

## Riscos

| Risco | Mitigação |
|---|---|
| Perder até 60s de amostras em queda abrupta | flush no fechamento do painel e no shutdown; risco documentado |
| Custos de WMI/processos no notebook saturado | nenhuma coleta nova; processos 1x/30s fora do loop |
| Antivírus amplificando o I/O | 1 open/write/close por minuto, não por amostra |
| Hibernação/desligamento | relatório mostra lacuna, nunca interpola |
| Relatório gigante para e-mail | agregação + downsample 1 ponto/min |
| Só gravar acima do limiar ⇒ não há "normal" para comparar | aceito; o relatório fala em episódios, não em média |
| Relatório descrever o limiar errado para dados antigos | dívida #1 — correção no cabeçalho do dia antes de usar o relatório como prova formal |

## Ordem de implementação sugerida

1. Config (DTO + cache TTL + aba Monitoramento) — permite ligar/desligar durante o desenvolvimento.
2. `UsoRecursosService` (fila + buffer + limiar) e `UsoJsonService` (JSONL por dia, retenção).
3. Amostra de processos: `ObterAmostraCpu()` no `IProcessosColetor` + helper de delta compartilhado.
4. `GET /api/uso/dias` e validação em uso real por 1 dia.
5. `RelatorioUsoService` + endpoints + botão na aba.
6. Testes unitários pela skill `pipeline` (agregação de episódios, downsample, nome do arquivo por dia, retenção).

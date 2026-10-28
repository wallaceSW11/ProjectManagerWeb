# Monitoramento — detalhe técnico por plataforma

Documento de referência do painel `/monitoramento`. O fluxo (arquitetura, ciclo, arquivos) está em `.opencode/flows/monitoramento.md`.
Decisões e histórico: `docs/monitoramento-plan.md`, `docs/monitoramento-analise.md`, `diagnostico-temperatura-cpu.md`.

## Particularidades Windows

| Métrica | Fonte | Detalhe |
|---------|-------|---------|
| Nome do SO | WMI `Win32_OperatingSystem.Caption` | `RuntimeInformation.OSDescription` retorna versão do kernel ("Microsoft Windows 10.0.26200" no Win 11) — por isso usa WMI e remove o prefixo "Microsoft ". Cacheado após a 1ª leitura |
| Temperatura CPU | WMI ACPI: `MSAcpi_ThermalZoneTemperature` (namespace `root\WMI` — o padrão do `ManagementObjectSearcher` é `root\cimv2`, então o escopo precisa ser explícito) → `Win32_PerfFormattedData_Counters_ThermalZoneInformation` (décimos de Kelvin) | Sem driver externo: o PMW não instala nada. Consulta no máximo a cada 30s e é desativada em definitivo quando nunca retorna leitura (exibe `--` e loga um aviso único). A maioria dos notebooks não expõe essas classes — `--` é o esperado |
| CPU % | `Processor Information\% Processor Utility` via `PerformanceCounter` — mesma métrica do Gerenciador de Tarefas. Fallback: `GetSystemTimes` (kernel32) = `% Processor Time` | `% Processor Utility` é escalado pela frequência (`% Processor Time × frequência atual / base`) e capado em 100, igual ao Gerenciador de Tarefas (MS KB 3200459). Por isso diverge de Process Explorer/`top`/Perfmon `% Processor Time`, que medem só ocupação de núcleo. A 1ª leitura do contador retorna 0 e cai no fallback; se a categoria não existir, o coletor usa `GetSystemTimes` (delta entre amostras, 1ª amostra null) e loga um aviso único |
| RAM | `GlobalMemoryStatusEx` (kernel32) | Total e disponível |
| Frequência CPU | `Win32_Processor.MaxClockSpeed` × `% Processor Performance` | Fallback: `CurrentClockSpeed` via WMI, no máximo a cada 30s |
| Velocidade RAM | `Win32_PhysicalMemory` | `ConfiguredClockSpeed` → fallback `Speed`. Cacheado após a 1ª leitura |
| Temperatura disco | IOCTL `IOCTL_STORAGE_QUERY_PROPERTY` + `StorageDeviceTemperatureProperty` (P/Invoke `kernel32`) | Abre `\\.\PhysicalDrive0..15` com `FILE_READ_ATTRIBUTES` e usa a maior temperatura entre as entradas. Sem driver e sem admin (quando o driver de storage permite). Consulta no máximo a cada 10s; se nunca retornar leitura, é desativada em definitivo com log único. O `PropertyId` correto é **52** (`StorageDeviceIoCapabilityProperty` = 48 no header do SDK). O header `STORAGE_TEMPERATURE_DATA_DESCRIPTOR` tem 24 bytes e cada `STORAGE_TEMPERATURE_INFO` tem 16 bytes (`Temperature` no offset +2) |
| I/O do disco | `IOCTL_DISK_PERFORMANCE` em `\\.\PhysicalDrive0..15` | Lê `DISK_PERFORMANCE` (88 bytes): `BytesRead`, `BytesWritten`, `ReadTime`/`WriteTime`/`IdleTime` em 100ns, `ReadCount`. Calcula por delta (1s): bytes/s de leitura e escrita, atividade % = (ΔReadTime+ΔWriteTime)/Δtempo, latência de leitura ms = ΔReadTime/ΔReadCount. Handles abertos uma vez (mantém os contadores habilitados) |

## Particularidades Linux

| Métrica | Fonte |
|---------|-------|
| Nome do SO | `/etc/os-release` `PRETTY_NAME` → fallback `RuntimeInformation.OSDescription`. Cacheado após a 1ª leitura |
| CPU % | `/proc/stat` (delta da linha `cpu `) |
| RAM | `/proc/meminfo` (`MemTotal`, `MemAvailable` × 1024) |
| Temperatura | `/sys/class/thermal` (type cpu/pkg) → `/sys/class/hwmon` (k10temp/coretemp) |
| Nome/freq CPU | `/proc/cpuinfo` → fallback `scaling_cur_freq`. Nome cacheado, frequência relida a cada ciclo |
| Velocidade RAM | SPD DDR4 via sysfs `/sys/bus/i2c/devices/*-005?/eeprom` (o kernel expõe o `ee1004` como `-r--r--r--`, sem root). `tCKAVGmin` = byte 18 × 125 ps + byte 125 (fine, com sinal); converte para MT/s (`2 × 1000 / tCK`) com o arredondamento JEDEC do `decode-dimms` (7,5/divisor para DDR3-1866+); usa o maior valor entre os pentes. Tipos que não sejam DDR4 (`byte 2` diferente de `0x0C`/`0x0E`) são ignorados. Cacheado na 1ª leitura |
| I/O do disco | `/proc/diskstats` — soma apenas dispositivos listados em `/sys/block` (evita contar partição + disco); lista revalidada a cada 30s. Campos: reads completed (idx 3), setores lidos (5) ×512, ms lendo (6), setores escritos (9) ×512, ms ocupado (12). Delta por amostra → bytes/s, atividade % = ΔmsOcupado/Δms, latência de leitura = ΔmsLendo/Δreads |

## Detalhes de implementação dos coletores

- Windows: dados imutáveis cacheados e consultas WMI com TTL de 30s; handles de disco abertos uma vez (I/O e temperatura); `DriveInfo` da raiz cacheado no `DiscoColetor`.
- Linux: SO/nome da CPU/velocidade RAM cacheados; frequência relida a cada ciclo.
- Processos (Linux): `/proc/[pid]/stat` (utime+stime em delta) e `/proc/[pid]/status` (VmRSS); `USER_HZ` via sysconf. Nome via `/proc/[pid]/exe` (fallback argv[0] → comm) — Chromium reescreve argv[0] dos filhos com argumentos embutidos, por isso `exe` é a fonte primária.
- Processos (Windows): `Process.GetProcesses()`; CPU% por delta de `TotalProcessorTime` (2 amostras, 500ms), RAM por `WorkingSet64`.

## Registro de uso

`UsoRecursosService` grava o snapshot do ciclo atual — o mesmo valor exibido no painel, sem defasagem de um ciclo. Cada amostra carrega `metricaCpu`:

| Plataforma | Valor | Significado |
|------------|-------|-------------|
| Windows | `utility` | `% Processor Utility` — escalado pela frequência, igual ao Gerenciador de Tarefas |
| Linux | `processor-time` | ocupação de núcleo medida em `/proc/stat` |

Amostras gravadas antes da troca de métrica não têm o campo — são `% Processor Time`. O relatório exibe "não registrada" e não devem ser comparadas com as novas. A retenção padrão de 30 dias elimina a mistura.

`PmwCpu` continua sendo tempo de processador do próprio PMW, sem escalonamento por frequência — é a medição do custo real do processo e alimenta o campo "Duração da coleta"/"CPU do PMW" no relatório.

## Frontend

- `MonitoramentoView.vue` monta `LayoutPainelEsportivo.vue` (layout único; o padrão anterior foi removido).
- Animação de entrada: ao montar (acesso ou F5), os dois ContaGiros fazem o bate-e-volta de carro (agulha 0→100→0, via `requestAnimationFrame`, sem transição CSS) enquanto os campos numéricos ficam zerados; quando ambos concluem, os valores reais entram. Depois da entrada, as atualizações de 1s usam transições CSS no ponteiro (`transform`) e no arco (`stroke-dashoffset` com `pathLength="1"`), sem rAF contínuo.
- ContaGiros é clicável → abre `ModalTopProcessos` (polling 2s enquanto aberto). Dialog de **80% de largura** (máx. 640px) e **altura total**: `content-class="conteudo-modal-processos"` + regra global (`.v-dialog > .v-overlay__content.conteudo-modal-processos`) zera a margem de 24px e o `max-height` do Vuetify — sem isso o modal nunca chega a 100% (`VDialog.css` aplica `margin: 24px` e `max-height: calc(100% - 48px)`). Cabeçalho compacto (`padding 3px 10px`, título `0.72rem`, ícone/fechar 16px). A lista rola dentro do modal; cada processo ocupa uma linha única — posição, nome com ellipsis e, no fim, o percentual (e GB, no modo RAM); sem barra de progresso.
- Tela cheia automática no celular: ao montar o painel, se for dispositivo móvel de verdade (`utils/dispositivoMovel.ts`: `navigator.userAgentData.mobile`, fallback por UA) e a Fullscreen API existir, chama `requestFullscreen()`. Sem gesto do usuário o navegador rejeita — nesse caso fica armado um `pointerdown` de uso único, e o primeiro toque entra em tela cheia. Detecção por **dispositivo**, não por largura: notebook com janela pequena ou devtools dockado não dispara. No iOS Safari a API não existe em elemento algum (`suportaTelaCheia` falso) e nada é chamado.
- Velocidade da RAM: rótulo acima do ContaGiros da RAM, exibido só quando o backend entrega `ramVelocidadeMhz`.
- I/O do disco: no grupo DISCO, abaixo da temperatura, em duas linhas — `mdi-arrow-down` (leitura) e `mdi-arrow-up` (escrita); `mdi-pulse` (atividade %) e `mdi-timer-outline` (latência de leitura ms). `--` quando o backend não entrega.
- Métricas sem leitura mostram `--`. Formatação pt-BR (2 casas, vírgula): `utils/formatarNumero.ts` (`formatarDecimal`).

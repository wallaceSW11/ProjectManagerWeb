# Monitoramento — detalhe técnico por plataforma

Documento de referência do painel `/monitoramento`. O fluxo (arquitetura, ciclo, arquivos) está em `.opencode/flows/monitoramento.md`.
Decisões e histórico: `docs/monitoramento-plan.md`, `docs/monitoramento-analise.md`, `diagnostico-temperatura-cpu.md`.

## Particularidades Windows

| Métrica | Fonte | Detalhe |
|---------|-------|---------|
| Nome do SO | WMI `Win32_OperatingSystem.Caption` | `RuntimeInformation.OSDescription` retorna versão do kernel ("Microsoft Windows 10.0.26200" no Win 11) — por isso usa WMI e remove o prefixo "Microsoft ". Cacheado após a 1ª leitura |
| Temperatura CPU | WMI ACPI: `MSAcpi_ThermalZoneTemperature` (namespace `root\WMI` — o padrão do `ManagementObjectSearcher` é `root\cimv2`, então o escopo precisa ser explícito) → `Win32_PerfFormattedData_Counters_ThermalZoneInformation` (décimos de Kelvin) | Sem driver externo: o PMW não instala nada. Consulta no máximo a cada 30s e é desativada em definitivo quando nunca retorna leitura (exibe `--` e loga um aviso único). A maioria dos notebooks não expõe essas classes — `--` é o esperado |
| CPU % | `GetSystemTimes` (kernel32) | Delta entre amostras; 1ª amostra retorna null |
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

- Windows: dados imutáveis cacheados e consultas WMI com TTL de 30s; handles de disco abertos uma vez.
- Linux: SO/nome da CPU/velocidade RAM cacheados; frequência relida a cada ciclo.
- Processos (Linux): `/proc/[pid]/stat` (utime+stime em delta) e `/proc/[pid]/status` (VmRSS); `USER_HZ` via sysconf. Nome via `/proc/[pid]/exe` (fallback argv[0] → comm) — Chromium reescreve argv[0] dos filhos com argumentos embutidos, por isso `exe` é a fonte primária.
- Processos (Windows): `Process.GetProcesses()`; CPU% por delta de `TotalProcessorTime` (2 amostras, 500ms), RAM por `WorkingSet64`.

## Frontend

- `MonitoramentoView.vue` monta `LayoutPainelEsportivo.vue` (layout único; o padrão anterior foi removido).
- Animação de entrada: ao montar (acesso ou F5), os dois ContaGiros fazem o bate-e-volta de carro (agulha 0→100→0, via `requestAnimationFrame`, sem transição CSS) enquanto os campos numéricos ficam zerados; quando ambos concluem, os valores reais entram. Depois da entrada, as atualizações de 1s usam transições CSS no ponteiro (`transform`) e no arco (`stroke-dashoffset` com `pathLength="1"`), sem rAF contínuo.
- ContaGiros é clicável → abre `ModalTopProcessos` (v-dialog 80% mobile-first, polling 2s enquanto aberto).
- Velocidade da RAM: rótulo acima do ContaGiros da RAM, exibido só quando o backend entrega `ramVelocidadeMhz`.
- I/O do disco: no grupo DISCO, abaixo da temperatura, em duas linhas — `mdi-arrow-down` (leitura) e `mdi-arrow-up` (escrita); `mdi-pulse` (atividade %) e `mdi-timer-outline` (latência de leitura ms). `--` quando o backend não entrega.
- Métricas sem leitura mostram `--`. Formatação pt-BR (2 casas, vírgula): `utils/formatarNumero.ts` (`formatarDecimal`).

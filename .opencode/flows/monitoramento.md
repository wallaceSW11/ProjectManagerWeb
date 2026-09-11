# Fluxo: Monitoramento da Máquina (`/monitoramento`)

## Conceito

Painel de monitoramento local acessível pela rede (celular na mesma Wi-Fi acessa `http://IP-do-PC:2025/monitoramento`).
Comunicação via WebSocket puro (`System.Net.WebSockets`), 1 snapshot por segundo.
Coleta acontece **somente enquanto houver cliente conectado** — zero consumo ocioso.

## Arquitetura — backend

```
MonitoramentoController        → GET /api/monitoramento/ws (handshake WebSocket)
MonitoramentoService           → singleton: gerencia sockets + ciclo de coleta
ColetorComposto                → mescla snapshots dos coletores (with { ... })
├── CpuRamColetor              → CPU, RAM, SO, nome/frequência/temperatura CPU, velocidade RAM, temp disco, swap
│   ├── ICpuRamColetor         → interface por plataforma (registrada por OS no Program.cs)
│   ├── WindowsCpuRamColetor   → P/Invoke kernel32 + WMI (SO, RAM, frequência, temperatura ACPI); dados imutáveis cacheados e consultas WMI com TTL de 30s; nenhum driver externo (ver diagnóstico da temperatura)
│   └── LinuxCpuRamColetor     → /proc e /sys (swap via /proc/meminfo, temp disco via hwmon nvme, velocidade RAM via SPD i2c); SO/nome da CPU/velocidade RAM cacheados
├── DiscoColetor               → disco da raiz do diretório de trabalho
├── DiscoIOColetor             → I/O do disco físico (leitura/escrita por segundo, atividade %, latência de leitura ms)
│   ├── IDiscoIOColetor        → interface por plataforma (registrada por OS no Program.cs)
│   ├── WindowsDiscoIOColetor  → IOCTL_DISK_PERFORMANCE em `\\.\PhysicalDrive0..15` (nativo, sem admin); handles mantidos abertos e deltas por amostra
│   └── LinuxDiscoIOColetor    → `/proc/diskstats` filtrando dispositivos de `/sys/block` (lista revalidada a cada 30s)
└── RedeColetor                → download/upload em bytes/segundo (delta entre amostras)
    ├── IRedeColetor           → interface por plataforma
    ├── WindowsRedeColetor     → NetworkInterface.GetIPStatistics() somando interfaces ativas (sem loopback); lista de interfaces revalidada a cada 30s
    └── LinuxRedeColetor       → /proc/net/dev somando todas as interfaces (sem lo)

ProcessosService               → GET /api/monitoramento/processos/top/{tipo} (cpu|ram), sob demanda
└── IProcessosColetor          → top 10 processos por CPU ou RAM, por plataforma
    ├── WindowsProcessosColetor → Process.GetProcesses(): CPU% por delta de TotalProcessorTime (2 amostras, 500ms), RAM por WorkingSet64
    └── LinuxProcessosColetor   → /proc/[pid]/stat (utime+stime em delta) e /proc/[pid]/status (VmRSS); USER_HZ via sysconf. Nome do processo via /proc/[pid]/exe (fallback argv[0] → comm) — Chromium reescreve argv[0] dos filhos com argumentos embutidos, por isso exe é a fonte primária
```

Swap é exclusivo do Linux (Windows retorna nulo e o frontend oculta o bloco).

Regras do ciclo:

- Contador de sockets 0→1 inicia `PeriodicTimer` (1s); 1→0 para o loop.
- O snapshot carrega somente métricas — `MonitoramentoService` é quem decide quando coletar e para quem enviar (coletores nunca sabem de transporte).
- Novo coletor: implementar `IColetorMetricas`, adicionar campos `double?`/`long?` no `MonitoramentoSnapshotDTO` e registrar no `ColetorComposto` (Program.cs). Nada mais muda.

## DTO

`MonitoramentoSnapshotDTO` — record com campos opcionais (`double?`, `long?`). Campos novos não quebram nada.
Serializado camelCase para o frontend.

## Particularidades Windows

| Métrica | Fonte | Detalhe |
|---------|-------|---------|
| Nome do SO | WMI `Win32_OperatingSystem.Caption` | `RuntimeInformation.OSDescription` retorna versão do kernel ("Microsoft Windows 10.0.26200" no Win 11) — por isso usa WMI e remove o prefixo "Microsoft ". Resultado cacheado após a 1ª leitura |
| Temperatura CPU | WMI ACPI: `MSAcpi_ThermalZoneTemperature` (namespace `root\WMI` — o padrão do `ManagementObjectSearcher` é `root\cimv2`, então o escopo precisa ser explícito) → `Win32_PerfFormattedData_Counters_ThermalZoneInformation` (décimos de Kelvin) | Sem driver externo: o PMW não instala nada (decisão registrada em `diagnostico-temperatura-cpu.md`). A consulta roda no máximo a cada 30s e é desativada em definitivo quando nunca retorna leitura (nesse caso exibe `--` e loga um aviso único). A maioria dos notebooks não expõe essas classes — `--` é o comportamento esperado. |
| CPU % | `GetSystemTimes` (kernel32) | Delta entre amostras; 1ª amostra retorna null |
| RAM | `GlobalMemoryStatusEx` (kernel32) | Total e disponível |
| Frequência CPU | `Win32_Processor.MaxClockSpeed` × `% Processor Performance` | Fallback: `CurrentClockSpeed` via WMI, no máximo a cada 30s |
| Velocidade RAM | `Win32_PhysicalMemory` | `ConfiguredClockSpeed` → fallback `Speed`. Resultado cacheado após a 1ª leitura |
| Temperatura disco | IOCTL `IOCTL_STORAGE_QUERY_PROPERTY` + `StorageDeviceTemperatureProperty` (P/Invoke `kernel32`) | Abre `\\.\PhysicalDrive0..15` com `FILE_READ_ATTRIBUTES` e usa a maior temperatura entre as entradas. Sem driver e sem admin (quando o driver de storage permite). Consulta no máximo a cada 10s; se nunca retornar leitura, é desativada em definitivo com log único. O `PropertyId` correto é **52** (`StorageDeviceIoCapabilityProperty = 48` no header do SDK; `StorageDeviceTemperatureProperty` = 52). O header `STORAGE_TEMPERATURE_DATA_DESCRIPTOR` tem 24 bytes e cada `STORAGE_TEMPERATURE_INFO` tem 16 bytes (`Temperature` no offset +2) |
| I/O do disco | `IOCTL_DISK_PERFORMANCE` em `\\.\PhysicalDrive0..15` | Lê `DISK_PERFORMANCE` (88 bytes): `BytesRead`, `BytesWritten`, `ReadTime`/`WriteTime`/`IdleTime` em 100ns, `ReadCount`. Calcula por delta (1s): leitura/escrita bytes/s, atividade % = (ΔReadTime+ΔWriteTime)/Δtempo e latência de leitura ms = ΔReadTime/ΔReadCount. Handles abertos uma vez (mantém os contadores habilitados) |

## Particularidades Linux

| Métrica | Fonte |
|---------|-------|
| Nome do SO | `/etc/os-release` `PRETTY_NAME` → fallback `RuntimeInformation.OSDescription`. Resultado cacheado após a 1ª leitura |
| CPU % | `/proc/stat` (delta da linha `cpu `) |
| RAM | `/proc/meminfo` (`MemTotal`, `MemAvailable` × 1024) |
| Temperatura | `/sys/class/thermal` (type cpu/pkg) → `/sys/class/hwmon` (k10temp/coretemp) |
| Nome/freq CPU | `/proc/cpuinfo` → fallback `scaling_cur_freq`. Nome cacheado, frequência relida a cada ciclo |
| Velocidade RAM | SPD DDR4 via sysfs `/sys/bus/i2c/devices/*-005?/eeprom` (o kernel expõe o `ee1004` como `-r--r--r--`, sem root) | `tCKAVGmin` = byte 18 × 125 ps + byte 125 (fine, com sinal); converte para MT/s (`2 × 1000 / tCK`) com o arredondamento JEDEC do `decode-dimms` (7,5/divisor para DDR3-1866+); usa o maior valor entre os pentes. Tipos que não sejam DDR4 (`byte 2` diferente de `0x0C`/`0x0E`) são ignorados. Cacheado na 1ª leitura |
| I/O do disco | `/proc/diskstats` | Soma apenas os dispositivos listados em `/sys/block` (evita contar partição + disco). Campos: reads completed (idx 3), setores lidos (5) ×512, ms lendo (6), setores escritos (9) ×512, ms ocupado (12). Delta por amostra → bytes/s, atividade % = ΔmsOcupado/Δms e latência de leitura = ΔmsLendo/Δreads |

## Frontend

Rota `/monitoramento` → `MonitoramentoView.vue` → `LayoutPainelEsportivo.vue` (layout único, o padrão foi removido).

Animação de entrada: ao montar a tela (acesso ou F5), os dois ContaGiros fazem o bate-e-volta de carro (agulha 0→100→0, via `requestAnimationFrame`, sem transição CSS) enquanto os campos numéricos ficam zerados; quando ambos concluem, os valores reais entram. Depois da entrada, as atualizações de 1 em 1 segundo usam transições CSS no ponteiro (`transform`) e no arco (`stroke-dashoffset` com `pathLength="1"`), sem rAF contínuo. Métricas sem leitura mostram `--`.

```
services/monitoramentoService.ts → WebSocket (reconexão exponencial 2s → 30s) + REST top processos
stores/monitoramento.ts          → conexão, snapshot, status, processos (cpu/ram)
models/MonitoramentoModel.ts     → model com constructor + toDTO
views/MonitoramentoView.vue      → barra discreta + corpo do painel
components/monitoramento/
├── layouts/LayoutPainelEsportivo.vue → ContaGiros (SVG animado)
├── painel/ContaGiros.vue        → clicável (abre o modal de top processos)
└── ModalTopProcessos.vue        → v-dialog 80% mobile-first, polling 2s enquanto aberto
```

Velocidade da RAM: rótulo acima do ContaGiros da RAM (`LayoutPainelEsportivo.vue`), exibido somente quando o backend entrega `ramVelocidadeMhz` (Windows).

I/O do disco: no grupo DISCO, abaixo da temperatura, em duas linhas — a primeira com `mdi-arrow-down` (leitura) e `mdi-arrow-up` (escrita); a segunda com `mdi-pulse` (atividade %) e `mdi-timer-outline` (latência de leitura ms). `--` quando o backend não entrega a métrica.

URL do WebSocket: dev `ws://localhost:2024/api/monitoramento/ws`; prod `ws://{location.host}/api/monitoramento/ws` (mesma origem, sem CORS).

Formatação de disco %/GB e RAM GB: `utils/formatarNumero.ts` (`formatarDecimal` — pt-BR, 2 casas, vírgula).

## Arquivos envolvidos

```
backend/Program.cs                                              → registros DI
backend/src/Controllers/MonitoramentoController.cs
backend/src/Services/Monitoramento/MonitoramentoService.cs
backend/src/Services/Monitoramento/ProcessosService.cs
backend/src/Services/Monitoramento/IColetorMetricas.cs
backend/src/Services/Monitoramento/Coletores/*.cs               → inclui IProcessosColetor + Windows/LinuxProcessosColetor
backend/src/Services/Monitoramento/Coletores/IDiscoIOColetor.cs
backend/src/Services/Monitoramento/Coletores/DiscoIOColetor.cs
backend/src/Services/Monitoramento/Coletores/WindowsDiscoIOColetor.cs
backend/src/Services/Monitoramento/Coletores/LinuxDiscoIOColetor.cs
backend/src/DTOs/MonitoramentoSnapshotDTO.cs
backend/src/DTOs/ProcessoInfoDTO.cs
frontend/src/views/MonitoramentoView.vue
frontend/src/components/monitoramento/**/*.vue
frontend/src/stores/monitoramento.ts
frontend/src/services/monitoramentoService.ts
frontend/src/models/MonitoramentoModel.ts
frontend/src/utils/formatarNumero.ts
docs/monitoramento-plan.md / docs/monitoramento-analise.md      → histórico de decisões
diagnostico-temperatura-cpu.md                                   → diagnóstico e decisão sobre a temperatura no Windows
```

## Testes

`backend/tests/ProjectManagerWeb.Tests/Services/` — `MonitoramentoServiceTests.cs` (transição de estado, NSubstitute), `ColetorCompostoTests.cs`, `CpuRamColetorTests.cs`, `DiscoColetorTests.cs`, `LinuxCpuRamColetorTests.cs`.

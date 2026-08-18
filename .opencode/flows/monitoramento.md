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
├── CpuRamColetor              → CPU, RAM, SO, nome/frequência/temperatura CPU, velocidade RAM
│   ├── ICpuRamColetor         → interface por plataforma (registrada por OS no Program.cs)
│   ├── WindowsCpuRamColetor   → WMI + P/Invoke kernel32
│   └── LinuxCpuRamColetor     → /proc e /sys
└── DiscoColetor               → disco da raiz do diretório de trabalho
```

Regras do ciclo:

- Contador de sockets 0→1 inicia `PeriodicTimer` (1s); 1→0 para o loop.
- `ClientesConectados` e `ContadorSnapshots` são preenchidos pelo `MonitoramentoService` via `snapshot with { ... }` — coletores nunca sabem de transporte.
- Novo coletor: implementar `IColetorMetricas`, adicionar campos `double?`/`long?` no `MonitoramentoSnapshotDTO` e registrar no `ColetorComposto` (Program.cs). Nada mais muda.

## DTO

`MonitoramentoSnapshotDTO` — record com campos opcionais (`double?`, `long?`). Campos novos não quebram nada.
Serializado camelCase para o frontend.

## Particularidades Windows

| Métrica | Fonte | Detalhe |
|---------|-------|---------|
| Nome do SO | WMI `Win32_OperatingSystem.Caption` | `RuntimeInformation.OSDescription` retorna versão do kernel ("Microsoft Windows 10.0.26200" no Win 11) — por isso usa WMI e remove o prefixo "Microsoft " |
| Temperatura CPU | WMI `MSAcpi_ThermalZoneTemperature` | Vive no namespace `root\WMI` — o padrão do `ManagementObjectSearcher` é `root\cimv2`, então o escopo precisa ser explícito. Fallback: `Win32_PerfFormattedData_Counters_ThermalZoneInformation` (décimos de Kelvin). Se a placa não expõe o sensor via WMI, vem `--` |
| CPU % | `GetSystemTimes` (kernel32) | Delta entre amostras; 1ª amostra retorna null |
| RAM | `GlobalMemoryStatusEx` (kernel32) | Total e disponível |
| Frequência CPU | `Win32_Processor.MaxClockSpeed` × `% Processor Performance` | Fallback: `CurrentClockSpeed` |
| Velocidade RAM | `Win32_PhysicalMemory` | `ConfiguredClockSpeed` → fallback `Speed` |

## Particularidades Linux

| Métrica | Fonte |
|---------|-------|
| Nome do SO | `/etc/os-release` `PRETTY_NAME` → fallback `RuntimeInformation.OSDescription` |
| CPU % | `/proc/stat` (delta da linha `cpu `) |
| RAM | `/proc/meminfo` (`MemTotal`, `MemAvailable` × 1024) |
| Temperatura | `/sys/class/thermal` (type cpu/pkg) → `/sys/class/hwmon` (k10temp/coretemp) |
| Nome/freq CPU | `/proc/cpuinfo` → fallback `scaling_cur_freq` |

## Frontend

Rota `/monitoramento` → `MonitoramentoView.vue` → layout selecionável (padrão ou esportivo), persistido via `useLayoutMonitoramento`.

```
services/monitoramentoService.ts → wrapper WebSocket com reconexão exponencial (2s → 30s)
stores/monitoramento.ts          → conexão, snapshot, status
models/MonitoramentoModel.ts     → model com constructor + toDTO
views/MonitoramentoView.vue      → barra + seletor de layout
components/monitoramento/
├── layouts/LayoutPadrao.vue         → cards com v-progress-circular/linear
├── layouts/LayoutPainelEsportivo.vue → ContaGiros (SVG animado)
├── CardCircular.vue / CardMetrica.vue
└── painel/ContaGiros.vue
```

URL do WebSocket: dev `ws://localhost:2024/api/monitoramento/ws`; prod `ws://{location.host}/api/monitoramento/ws` (mesma origem, sem CORS).

Formatação de disco %/GB e RAM GB: `utils/formatarNumero.ts` (`formatarDecimal` — pt-BR, 2 casas, vírgula).

## Arquivos envolvidos

```
backend/Program.cs                                              → registros DI
backend/src/Controllers/MonitoramentoController.cs
backend/src/Services/Monitoramento/MonitoramentoService.cs
backend/src/Services/Monitoramento/IColetorMetricas.cs
backend/src/Services/Monitoramento/Coletores/*.cs
backend/src/DTOs/MonitoramentoSnapshotDTO.cs
frontend/src/views/MonitoramentoView.vue
frontend/src/components/monitoramento/**/*.vue
frontend/src/stores/monitoramento.ts
frontend/src/services/monitoramentoService.ts
frontend/src/models/MonitoramentoModel.ts
frontend/src/composables/useLayoutMonitoramento.ts
frontend/src/utils/formatarNumero.ts
docs/monitoramento-plan.md / docs/monitoramento-analise.md      → histórico de decisões
```

## Testes

`backend/tests/ProjectManagerWeb.Tests/Services/` — `MonitoramentoServiceTests.cs` (transição de estado, NSubstitute), `ColetorCompostoTests.cs`, `CpuRamColetorTests.cs`, `DiscoColetorTests.cs`, `LinuxCpuRamColetorTests.cs`.

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
│   ├── WindowsCpuRamColetor   → LibreHardwareMonitor (temperatura) + WMI (fallback) + P/Invoke kernel32
│   └── LinuxCpuRamColetor     → /proc e /sys
└── DiscoColetor               → disco da raiz do diretório de trabalho

ProcessosService               → GET /api/monitoramento/processos/top/{tipo} (cpu|ram), sob demanda
└── IProcessosColetor          → top 10 processos por CPU ou RAM, por plataforma
    ├── WindowsProcessosColetor → Process.GetProcesses(): CPU% por delta de TotalProcessorTime (2 amostras, 500ms), RAM por WorkingSet64
    └── LinuxProcessosColetor   → /proc/[pid]/stat (utime+stime em delta) e /proc/[pid]/status (VmRSS); USER_HZ via sysconf. Nome do processo via /proc/[pid]/exe (fallback argv[0] → comm) — Chromium reescreve argv[0] dos filhos com argumentos embutidos, por isso exe é a fonte primária
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
| Temperatura CPU | `LibreHardwareMonitorLib` (DLL mantida em `backend/libs/`, extraída do pacote NuGet oficial 0.9.6 — MPL-2.0) | Fonte primária: lê os sensores reais da CPU (MSR via driver WinRing0). `Computer` aberto lazy na primeira coleta e atualizado a cada snapshot; retorna a maior temperatura entre os sensores do hardware CPU. **Requer execução como administrador** — sem elevação o driver não carrega e cai no fallback WMI. Fallback: WMI `MSAcpi_ThermalZoneTemperature` (namespace `root\WMI` — o padrão do `ManagementObjectSearcher` é `root\cimv2`, então o escopo precisa ser explícito) e `Win32_PerfFormattedData_Counters_ThermalZoneInformation` (décimos de Kelvin). Se nada retornar sensor, vem `--`. A DLL é referenciada por `HintPath` (não PackageReference) porque o pacote só publica asset de runtime para RIDs Windows — como referência de pacote, o publish Linux ficava sem a DLL e o app crashava no scan de controllers; por HintPath ela entra no deps.json e vai em todos os builds. Dependências (HidSharp, DiskInfoToolkit, RAMSPDToolkit-NDD, Mono.Posix, System.IO.Ports) continuam via NuGet. |
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

Rota `/monitoramento` → `MonitoramentoView.vue` → `LayoutPainelEsportivo.vue` (layout único, o padrão foi removido).

Animação de entrada: ao montar a tela (acesso ou F5), os dois ContaGiros fazem o bate-e-volta de carro (agulha 0→100→0) enquanto os campos numéricos ficam zerados; quando ambos concluem, os valores reais entram. Métricas sem leitura mostram `--`.

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
backend/src/DTOs/MonitoramentoSnapshotDTO.cs
backend/src/DTOs/ProcessoInfoDTO.cs
frontend/src/views/MonitoramentoView.vue
frontend/src/components/monitoramento/**/*.vue
frontend/src/stores/monitoramento.ts
frontend/src/services/monitoramentoService.ts
frontend/src/models/MonitoramentoModel.ts
frontend/src/utils/formatarNumero.ts
docs/monitoramento-plan.md / docs/monitoramento-analise.md      → histórico de decisões
```

## Testes

`backend/tests/ProjectManagerWeb.Tests/Services/` — `MonitoramentoServiceTests.cs` (transição de estado, NSubstitute), `ColetorCompostoTests.cs`, `CpuRamColetorTests.cs`, `DiscoColetorTests.cs`, `LinuxCpuRamColetorTests.cs`.

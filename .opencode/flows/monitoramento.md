# Fluxo: Monitoramento da Máquina (`/monitoramento`)

Painel local acessível pela rede, via WebSocket, com 1 snapshot por segundo. Coleta só enquanto houver cliente conectado.
Detalhe técnico por plataforma (WMI, ioctl, /proc, /sys): `docs/monitoramento-fluxo.md`.

## Arquitetura — backend

```
MonitoramentoController   → GET /api/monitoramento/ws (handshake)
MonitoramentoService      → singleton: sockets + ciclo de coleta
ColetorComposto           → mescla snapshots (with { ... })
├── CpuRamColetor         → ICpuRamColetor → Windows* (PerformanceCounter + P/Invoke + WMI) | Linux* (/proc, /sys)
├── DiscoColetor          → disco da raiz do diretório de trabalho
├── DiscoIOColetor        → IDiscoIOColetor → Windows* (IOCTL_DISK_PERFORMANCE) | Linux* (/proc/diskstats)
└── RedeColetor           → IRedeColetor → Windows* (GetIPStatistics) | Linux* (/proc/net/dev)

ProcessosService          → GET /api/monitoramento/processos/top/{cpu|ram}
└── IProcessosColetor     → top 10 processos por CPU ou RAM
```

Regras do ciclo:

- Contador de sockets 0→1 inicia `PeriodicTimer` (1s); 1→0 para o loop.
- O snapshot carrega só métricas — `MonitoramentoService` decide quando coletar e para quem enviar (coletor não conhece transporte).
- Novo coletor: implementar `IColetorMetricas`, adicionar campos `double?`/`long?` no `MonitoramentoSnapshotDTO` e registrar no `ColetorComposto` (`Program.cs`).
- Swap é exclusivo do Linux (Windows retorna nulo e o frontend oculta o bloco).
- Interface por plataforma registrada por OS no `Program.cs`.

## DTO

`MonitoramentoSnapshotDTO` — record com campos opcionais (`double?`, `long?`). Campo novo não quebra nada. Serializado camelCase.

## Frontend

```
services/monitoramentoService.ts → WebSocket (reconexão 2s → 30s) + REST top processos
stores/monitoramento.ts          → conexão, snapshot, status, processos (cpu/ram)
models/MonitoramentoModel.ts     → constructor + toDTO
views/MonitoramentoView.vue      → barra discreta + `LayoutPainelEsportivo.vue`
components/monitoramento/        → ContaGiros (SVG), ModalTopProcessos
```

WebSocket: dev `ws://localhost:2024/api/monitoramento/ws`; prod `ws://{location.host}/api/monitoramento/ws`.

## Arquivos envolvidos

```
backend/Program.cs                                              → registros DI por OS
backend/src/Controllers/MonitoramentoController.cs
backend/src/Services/Monitoramento/MonitoramentoService.cs
backend/src/Services/Monitoramento/ProcessosService.cs
backend/src/Services/Monitoramento/IColetorMetricas.cs
backend/src/Services/Monitoramento/Coletores/*.cs
backend/src/DTOs/MonitoramentoSnapshotDTO.cs
backend/src/DTOs/ProcessoInfoDTO.cs
frontend/src/views/MonitoramentoView.vue
frontend/src/components/monitoramento/**/*.vue
frontend/src/stores/monitoramento.ts
frontend/src/services/monitoramentoService.ts
frontend/src/models/MonitoramentoModel.ts
frontend/src/utils/formatarNumero.ts
backend/tests/ProjectManagerWeb.Tests/Services/                  → MonitoramentoServiceTests, ColetorCompostoTests, CpuRamColetorTests, DiscoColetorTests, LinuxCpuRamColetorTests
```

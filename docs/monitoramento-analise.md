# Análise — Monitoramento em Tempo Real (`/monitoramento`)

> Documento gerado em 11/08/2026. Análise pré-implementação — nenhum arquivo de código foi alterado.

## 1. A ideia

Uma nova rota `/monitoramento` no PMW que vira um painel acessível pela rede local (inicialmente um celular Android na mesma Wi-Fi). O backend coleta métricas do computador **somente enquanto houver cliente conectado**, enviando um snapshot por segundo via comunicação em tempo real. A implementação é incremental:

- **Fase 1:** rota + transporte em tempo real + detecção de conexão/desconexão + payload de teste.
- **Fase 2:** CPU e RAM, cross-platform (Windows/Linux), ~1s de frequência.
- **Futuro:** disco, GPU, temperatura, rede, bateria, processos etc. — a arquitetura precisa permitir plugar novos coletores sem refatorar.

Restrições: sem projeto novo, sem serviço externo, sem banco, sem cloud, sem dependências desnecessárias, impacto mínimo de CPU/memória, e coleta iniciada/encerrada conforme existência de clientes.

## 2. Como o PMW funciona hoje (pontos relevantes)

### Backend (.NET 9, um único processo Kestrel)

- `Program.cs` é minimal hosting: `WebApplication.CreateBuilder` com `WebRootPath = "frontend"`. Registro de serviços por **DI com `AddSingleton`** (padrão: registrar explicitamente cada serviço). Providers de plataforma via `IShellProvider` (`WindowsShellProvider`/`LinuxShellProvider` escolhido por `OperatingSystem.IsWindows()`).
- Porta fixa **2025** em produção (`Porta` no appsettings), com `app.Urls.Add("http://*:2025")` — **já escuta em todas as interfaces**, ou seja, já é acessível pela rede local. Em dev: `http://localhost:2024` (launchSettings).
- **Static files + SPA fallback customizado** no final do pipeline: se o path não começa com `/api` e não é um arquivo existente, serve `frontend/index.html`. É isso que vai tornar `/monitoramento` acessível automaticamente como rota SPA.
- `UseCors("AllowAll")`, `UseHttpsRedirection()` (sem porta HTTPS configurada, não redireciona — hoje funciona só HTTP), `MapControllers`, `MapOpenApi` só em dev.
- Identificação de plataforma: `OperatingSystem.IsWindows()` no backend, exposto ao frontend via `GET /api/versao/features` (`os: "windows"|"linux"`, flags `iis`, `deploy`), consumido pela store Pinia `features.ts`.
- Migrations rodam no startup via `MigrationService`; persistência JSON com `SemaphoreSlim(1,1)` por JsonService.
- `ShellExecute` é estático configurado no startup (padrão de configuração de serviço: `Configure(...)` chamado em `Program.cs`).
- **Não existe** nenhum uso de WebSocket, SignalR, SSE, `IHostedService` ou background task no projeto hoje.

### Frontend (Vue 3 + Vuetify + Pinia)

- Rotas em `router/index.ts` com lazy loading (`component: () => import(...)`). **O plugin `vite-plugin-pwa` já está instalado e configurado** (manifest, `registerType: 'autoUpdate'`) — o requisito PWA já tem infraestrutura parcial pronta.
- `BaseApiService` resolve a base URL: dev → `http://localhost:2024/api`, prod → `/api` (mesma origem). Esse padrão serve de referência para a URL do WebSocket.
- Stores Pinia Options API; components consomem stores, nunca services diretamente.
- `SnackbarNotificacao` + `eventBus` (mitt) para notificações.

### Builds/CI

O frontend é buildado e o `dist` é copiado para `backend/frontend/` (o csproj inclui `frontend/**` no output via `Content` + `LinkBase frontend`). O release publica self-contained `win-x64` e `linux-x64` e zipa com a pasta `infra/`. **Nenhuma mudança de pipeline é necessária** para a nova rota — ela entra no bundle naturalmente.

### Start/stop

Windows via `pmw.ps1` (Start-Process do exe), Linux via systemd user service (`pmw.service`). Nenhum ciclo de vida customizado no app (sem `IHostedService`, sem shutdown hook além do padrão do Kestrel).

### Testes

xUnit + FluentAssertions + NSubstitute, testando principalmente JsonServices com arquivos temporários e Utils. `InternalsVisibleTo` para o assembly de testes.

## 3. O que pode ser reutilizado

| Parte existente | Uso |
|---|---|
| SPA fallback do `Program.cs` | `/monitoramento` como rota Vue funciona sem tocar em nada — o fallback serve `index.html` |
| Escuta em `http://*:2025` | Acesso pela rede local já é possível; só precisa de exceção no firewall do Windows |
| `OperatingSystem.IsWindows()` + `features` endpoint | Detecção de plataforma para os coletores e para esconder/mostrar features no frontend |
| Padrão `IShellProvider` (interface + impl por OS + registro condicional no DI) | **Modelo de referência** para criar coletores de métricas por plataforma |
| `vite-plugin-pwa` já instalado | Base para o PWA (com ressalvas de HTTPS, ver riscos) |
| Padrão de stores Pinia + `BaseApiService` (dev vs prod) | Store de monitoramento e resolução da URL do WebSocket |
| `SemaphoreSlim(1,1)` | Proteção de concorrência no serviço de conexões |
| Estrutura de testes (JsonService/Utils com fixtures) | Testar coletor Linux com `/proc` fake e o serviço de conexões |
| DTOs `XDTO` (record) | `MonitoramentoSnapshotDTO` extensível |

## 4. Arquitetura recomendada

### Tecnologia de transporte: **WebSocket puro** (`System.Net.WebSockets`)

**Por quê:** é parte do framework ASP.NET Core (zero dependências novas — nem NuGet, nem npm, o navegador tem API nativa `WebSocket`), bidirecional, detecção nativa de desconexão (CloseStatus/Abort), frames mínimos (~1 snapshot/segundo é trivial), e funciona sobre HTTP simples na rede local (`ws://192.168.x.x:2025`). Não requer HTTPS.

Comparação rápida:

- **SSE** (`text/event-stream`): unidirecional e mais simples, mas a detecção de desconexão é menos imediata e o navegador pausa a reconexão em algumas condições. Funcionaria, mas WebSocket é mais robusto.
- **SignalR**: resolve tudo, mas adiciona o pacote npm `@microsoft/signalr` no frontend e é mais "pesado" do que o caso pede (o usuário pediu solução leve e sem dependências desnecessárias).
- **Polling HTTP**: viola o requisito de ~1s com o mínimo de recursos.

### Estrutura proposta (backend)

```
backend/src/
├── Controllers/            (nada obrigatório — ver decisão abaixo)
├── Services/
│   └── Monitoramento/
│       ├── MonitoramentoService.cs        ← singleton: gerencia conexões + ciclo de coleta
│       ├── IColetorMetricas.cs            ← interface do coletor
│       ├── Coletores/
│       │   ├── MetricasTesteColetor.cs    ← Fase 1 (payload de teste)
│       │   ├── CpuRamColetor.cs           ← Fase 2
│       │   └── ... (fases futuras plugam aqui)
│       └── ...
├── DTOs/MonitoramentoSnapshotDTO.cs
└── Middleware/MonitoramentoWebSocketMiddleware.cs
```

- **`MonitoramentoService` (singleton, registrado no `Program.cs`):**
  - `ConcurrentDictionary<WebSocket, ...>` ou lista com lock para as conexões ativas;
  - contador de clientes com `SemaphoreSlim`/lock para concorrência;
  - **loop de coleta** com `PeriodicTimer(TimeSpan.FromSeconds(1))` + `CancellationTokenSource`; `Task.Run` iniciado quando o contador vai de 0→1 e cancelado quando vai de 1→0 (com `await` do loop finalizado antes de retornar, para não deixar task órfã);
  - broadcast: serializa o snapshot uma vez e envia o mesmo buffer a todos os sockets (`SendAsync`, sem aguardar cada envio em sequência — tolerar falha por socket e removê-lo);
  - ping de manutenção do próprio WebSocket (o Kestrel já envia keep-alive, mas um ping explícito opcional ajuda em redes Wi-Fi instáveis).
- **`IColetorMetricas`**: `Task<MonitoramentoSnapshotDTO> ColetarAsync(CancellationToken)`. Cada fase futura é uma implementação nova — o snapshot DTO cresce com campos opcionais (`double?`), sem breaking change.
- **Handshake WebSocket**: middleware registrado **antes** do middleware fallback SPA no pipeline, interceptando `Path == "/monitoramento/ws"` (ver decisões) — ou um controller com rota `/api/monitoramento/ws` via `MapControllers` (que já roda antes do fallback, o que elimina o risco de ordem de middleware). O Kestrel precisa de `app.UseWebSockets()`.

### Estrutura proposta (frontend)

```
frontend/src/
├── views/MonitoramentoView.vue        ← rota '/monitoramento'
├── stores/monitoramento.ts            ← store Pinia: conexão, reconexão, snapshot, status
├── services/monitoramentoService.ts   ← wrapper do WebSocket (URL dev/prod)
├── models/MonitoramentoModel.ts       ← model com constructor + toDTO (padrão)
└── types/index.ts                     ← IMetricasSnapshot
```

- Store gerencia: `conectar()` (abre socket, handlers `onmessage`/`onclose`/`onerror`), `desconectar()` (fecha limpo), `snapshot`, `conectado`, `ultimaConexao`; **reconexão automática com backoff** (ex.: 2s → 4s → 8s, teto de 30s), parada quando o componente desmonta.
- URL: `ws://localhost:2024/monitoramento/ws` em dev; `ws://{location.host}/monitoramento/ws` em prod (mesma origem — sem problema de CORS).
- A view mostra cards com as métricas (Fase 1: apenas "conectado" + timestamp + payload de teste).

## 5. Fluxo de conexão/desconexão

1. Celular abre `http://192.168.x.x:2025/monitoramento` → o SPA fallback serve `index.html` → Vue monta `MonitoramentoView` → store abre `WebSocket`.
2. Handshake em `/monitoramento/ws` → `MonitoramentoService.ConexaoAdicionada(socket)` → contador 0→1 → inicia loop de coleta (`PeriodicTimer` 1s + token).
3. A cada tick: `ColetarAsync()` → snapshot JSON → `SendAsync` para todos os sockets.
4. Desconexão: `ReceiveAsync` retorna (fechamento do navegador, navegação, queda de Wi-Fi) ou lança exceção → remove o socket → contador 1→0 → cancela token e aguarda o loop encerrar → coleta para.
5. O `onBeforeUnmount`/`pagehide` da view fecha o socket limpo (`close(1000)`) — mas mesmo sem isso, o fechamento do TCP dispara a remoção no servidor.

## 6. Garantia de que a coleta só acontece com clientes

Toda a lógica vive no `MonitoramentoService`, que é a única fonte de verdade do estado:

- **Início:** primeiro `Adicionar` dispara o `CancellationTokenSource` e o `Task` do loop. Nada roda no app antes disso — zero timers, zero tasks, zero leitura de `/proc`/P/Invoke.
- **Parada:** último `Remover` cancela o token; o loop sai do `PeriodicTimer` (que respeita cancelamento) e o método `await` a task antes de retornar, garantindo que não fica task "zumbi".
- Idempotência protegida por lock: `iniciar`/`parar` não podem ser chamados em duplicidade.
- O loop nunca roda "pelo lado do servidor" sem socket — não existe timer de background global (e não usamos `IHostedService`, justamente para não rodar nada sem demanda).

## 7. Tecnologias/APIs para coleta (Windows e Linux, sem dependências)

### Fase 1 (teste)

Nenhuma coleta real — timestamp + contador + plataforma.

### Fase 2 — CPU/RAM, cross-platform, zero dependências novas

- **Linux:** ler `/proc/stat` (linha `cpu`) e `/proc/meminfo` (`MemTotal`, `MemAvailable`). Leitura de arquivo barata, padrão testável com fixtures. Cálculo de % de CPU com amostragem entre dois ticks (intervalo do próprio loop de 1s).
- **Windows:** P/Invoke nativo em `kernel32.dll` — `GetSystemTimes()` (CPU, sem privilégio administrativo) e `GlobalMemoryStatusEx()` (RAM total/disponível). Usar `LibraryImport`/`DllImport` com `partial` — não requer pacote NuGet.
- Alternativa a avaliar se houver resistência ao P/Invoke: `Microsoft.Extensions.Diagnostics.ResourceMonitoring` (NuGet oficial, cross-platform), mas **contraria a regra de "sem dependências sem necessidade"** — fica como plano B.

Cada coletor é uma classe que implementa `IColetorMetricas`; a Fase 2 adiciona `CpuRamColetor` e o snapshot ganha campos `CpuPercentual`, `RamUsadaBytes`, `RamTotalBytes`. Coletores futuros (disco, GPU, temperatura, rede) plugam na mesma interface — o `MonitoramentoService` não muda.

## 8. Riscos e problemas potenciais

1. **Middleware fallback SPA intercepta tudo que não é `/api`.** Se o WebSocket for exposto como rota não-`/api`, o handshake precisa ser tratado por um middleware registrado **antes** do fallback, senão o fallback responde HTML e a conexão falha. (Se for `/api/monitoramento/ws` via controller, o risco some — ver decisões.)
2. **PWA instalável exige HTTPS.** Service worker (instalação completa de PWA) só funciona em origins seguras. Em `http://192.168.x.x:2025` o Android Chrome não instala a PWA completa — apenas "adicionar à tela inicial" como atalho. O WebSocket, porém, **funciona normalmente em HTTP**. Isso afeta apenas o "instalar como PWA", não o monitoramento em si.
3. **Firewall do Windows** pode bloquear a porta 2025 na rede local (não é algo do código, é configuração do SO — vale documentar).
4. **Wi-Fi instável / tela do celular em background:** navegadores podem suspender timers e fechar sockets idle. Mitigação: keep-alive, reconexão automática com backoff, e reconexão no `visibilitychange` (página voltar ao foco).
5. **Vários clientes simultâneos:** o broadcast precisa tolerar socket morto sem derrubar o loop (try/catch por envio + remoção do socket falho).
6. **Shutdown do PMW:** Kestrel fecha os WebSockets no encerramento do processo; o loop deve observar o cancellation token do app (`ApplicationStopping`) para terminar limpo — detalhe barato de implementar.
7. **`UseHttpsRedirection`**: já convive com HTTP hoje (sem porta HTTPS não redireciona); nada muda para o monitoramento.
8. **Testabilidade do coletor Windows** em CI (que roda Linux): o P/Invoke não roda no CI — os testes unitários devem cobrir o coletor Linux (fixtures de `/proc`) e a lógica de cálculo; o Windows fica validado manualmente na máquina real (padrão já usado no projeto, ex.: IIS).

## 9. Pontos que precisam de decisão

1. **Caminho do WebSocket:** `/monitoramento/ws` via middleware dedicado (rota "bonita", exige ordem correta no pipeline) **ou** `/api/monitoramento/ws` via controller padrão (segue 100% o padrão de rotas do projeto, zero risco de middleware, mas fica "dentro" de /api). **Recomendo `/api/monitoramento/ws` via controller** — é o caminho que menos foge dos padrões atuais e não tem armadilha de ordem de pipeline.
2. **PWA:** aceitar a limitação de HTTP (instalação parcial — atalho + monitoramento funcionando) ou investir em HTTPS self-signed no futuro? **Recomendo:** Fase 1/2 sem HTTPS (monitoramento funciona), e tratar PWA completo como item futuro.
3. **Formato do snapshot:** DTO único com campos opcionais (`double?`) crescendo por fase — **recomendo sim**, evita breaking change no cliente.
4. **Frequência:** fixa em 1s (recomendo) ou configurável via appsettings para o futuro? (YAGNI: fixa em 1s.)
5. **Payload da Fase 1:** confirmar se basta timestamp + plataforma + contador + mensagem "conectado".

## 10. Partes da ideia inadequadas para a arquitetura atual

- **PWA "completa e instalável" na rede local via HTTP** é a única parte que conflita com a arquitetura atual (que é HTTP puro): service worker não instala sem HTTPS. A funcionalidade de monitoramento não depende disso — só o "instalar como PWA" fica limitado a atalho de tela inicial até decidirmos sobre HTTPS.
- **Nada mais** da ideia é inadequado: o PMW já é um servidor web local escutando em todas as interfaces, já serve SPA com fallback, já tem padrão de providers por plataforma e já tem o `vite-plugin-pwa`. A funcionalidade encaixa como mais uma rota + um serviço singleton, sem tocar em nenhuma funcionalidade existente (nenhum arquivo atual precisa mudar além de `Program.cs` para registrar rota/serviço, e `router/index.ts` + novos arquivos no frontend).

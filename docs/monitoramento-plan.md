# Plano de Implementação — Monitoramento em Tempo Real (`/monitoramento`)

> Baseado na [análise de arquitetura](./monitoramento-analise.md) aprovada. Terceira revisão — corrige race condition de ciclo de vida, self-await e validação de acesso LAN.

## Visão geral

Funcionalidade de painel de monitoramento local, acessível pela rede, com comunicação WebSocket pura (`System.Net.WebSockets`). Coleta de métricas acontece **somente com cliente conectado** — zero consumo ocioso.

### O que **não** será feito neste plano

- HTTPS / PWA completo com service worker definitivo (será tratado no futuro, se necessário)
- Coleta real de métricas na Fase 1 (somente payload de teste)
- Métricas de disco, GPU, rede, temperatura, bateria, processos, etc.

### Tecnologias

- **Transporte:** `System.Net.WebSockets` (nativo ASP.NET Core, zero dependências)
- **Coletores:** P/Invoke `kernel32` (Windows) e leitura de `/proc` (Linux), zero dependências
- **Temporizador:** `PeriodicTimer` + `CancellationToken`
- **Frontend:** WebSocket API nativa do navegador (zero dependências npm adicionais)

### Arquivos existentes modificados (ambas as fases)

- `backend/Program.cs` — registro de DI e `UseWebSockets`
- `frontend/src/router/index.ts` — adicionar rota `/monitoramento`
- `frontend/src/types/index.ts` — adicionar interface `IMonitoramentoSnapshot`

**Nenhum outro arquivo existente será alterado.**

### Acesso pela rede local — validação de infraestrutura

O backend em produção faz bind em todas as interfaces:

```csharp
// Program.cs linha 79-84
if (!app.Environment.IsDevelopment())
{
    var porta = app.Configuration.GetValue<int>("Porta", 2025);
    app.Urls.Clear();
    app.Urls.Add($"http://*:{porta}");
}
```

`http://*:2025` equivale a `http://0.0.0.0:2025` — o Kestrel escuta em **todas as interfaces de rede**. O notebook e o celular na mesma Wi-Fi acessam o mesmo servidor. O roteador Wi-Fi atribui um IP local ao notebook (ex.: `192.168.0.10`), e o celular acessa `http://192.168.0.10:2025/monitoramento`.

**`UseHttpsRedirection()` (linha 92):** sem porta HTTPS configurada no `app.Urls`, o middleware de redirecionamento não age. Nenhuma requisição HTTP é redirecionada. O handshake WebSocket via `ws://` funciona normalmente.

**CORS (`AllowAnyOrigin`):** WebSocket não é bloqueado por CORS (o handshake é uma requisição HTTP GET com headers `Upgrade`, mas a verificação de CORS do navegador não se aplica a WebSocket — a origem é verificada opcionalmente pelo servidor). De qualquer forma, como o frontend é servido pelo mesmo host, a origem é a mesma. Zero risco.

**Cenários validados:**

| Dispositivo | URL da página | URL do WebSocket |
|---|---|---|
| Notebook (localhost) | `http://localhost:2025/monitoramento` | `ws://localhost:2025/api/monitoramento/ws` |
| Notebook (IP local) | `http://192.168.0.10:2025/monitoramento` | `ws://192.168.0.10:2025/api/monitoramento/ws` |
| Celular (Wi-Fi) | `http://192.168.0.10:2025/monitoramento` | `ws://192.168.0.10:2025/api/monitoramento/ws` |

O frontend usa `location.host` para construir a URL do WebSocket — isso resolve automaticamente para `localhost:2025` ou `192.168.0.10:2025` conforme o que foi digitado na barra de endereços.

---

## Fase 1 — Infraestrutura e comunicação

**Objetivo:** rota `/monitoramento`, WebSocket funcional, payload de teste, detecção de conexão/desconexão, reconexão automática, interface com status completo. Validado no navegador do próprio notebook.

### 1.1 — Backend: DTO e interface do coletor

#### `backend/src/DTOs/MonitoramentoSnapshotDTO.cs` (novo)

```csharp
public sealed record MonitoramentoSnapshotDTO(
    DateTime Timestamp,
    string Plataforma,
    int ClientesConectados,
    int ContadorSnapshots,
    string Mensagem
)
```

`ClientesConectados` e `ContadorSnapshots` são preenchidos **exclusivamente** pelo `MonitoramentoService` após o coletor retornar o snapshot base, via `snapshot with { ... }`. O coletor nunca tem dependência de informações de transporte/WebSocket.

#### `backend/src/Services/Monitoramento/IColetorMetricas.cs` (novo)

```csharp
public interface IColetorMetricas
{
    Task<MonitoramentoSnapshotDTO> ColetarAsync(CancellationToken ct);
}
```

### 1.2 — Backend: Coletor de teste

#### `backend/src/Services/Monitoramento/Coletores/MetricasTesteColetor.cs` (novo)

Implementa `IColetorMetricas`. Retorna snapshot base com:

| Campo | Valor |
|---|---|
| `Timestamp` | `DateTime.UtcNow` |
| `Plataforma` | `"windows"` ou `"linux"` via `OperatingSystem.IsWindows()` |
| `Mensagem` | texto fixo ou incrementado a cada chamada (ex.: `"Snapshot #N"`) |
| `ClientesConectados` | `0` (sobrescrito pelo serviço) |
| `ContadorSnapshots` | `0` (sobrescrito pelo serviço) |

### 1.3 — Backend: MonitoramentoService

#### `backend/src/Services/Monitoramento/MonitoramentoService.cs` (novo)

**Registro:** `builder.Services.AddSingleton<MonitoramentoService>()` no `Program.cs`.

**Responsabilidades únicas:**
- Gerenciar a lista de WebSockets e transições de estado (iniciar/parar coleta)
- Broadcast sequencial do snapshot a cada ~1s
- A coleta de métricas em si é delegada ao `IColetorMetricas` injetado

#### Estado interno

```
_cicloSemaphore → SemaphoreSlim(1,1)    ← protege transições de ciclo de vida (iniciar/parar loop)
_socketsLock     → object                ← lock RÁPIDO (sem await) para proteger _sockets e _numeroSnapshots
_sockets         → List<WebSocket>       ← conexões ativas
_timer           → PeriodicTimer?        ← existe SOMENTE enquanto houver clientes; null quando parado
_cts             → CancellationTokenSource? ← existe SOMENTE enquanto houver clientes; null quando parado
_loopTask        → Task?                 ← task do loop ativo; null quando parado
_numeroSnapshots → int                   ← incrementado a cada tick
_coletor         → IColetorMetricas      ← injetado, nunca muda
_logger          → ILogger<MonitoramentoService> ← injetado
```

**`_timer`, `_cts` e `_loopTask` nunca ficam com valores não-nulos após a parada do ciclo.** Isso garante que recursos não ficam alocados sem necessidade e que um novo ciclo sempre cria instâncias novas (evita reutilização de CTS cancelado).

#### Solução para race condition: `SemaphoreSlim(1,1)` mantido durante `await`

O problema original: `lock` não pode ser mantido durante `await`. Se `PararLoopAsync` libera o lock antes de aguardar `_loopTask`, `AdicionarConexao` pode iniciar um novo loop enquanto o antigo ainda roda.

**Solução:** `SemaphoreSlim(1,1)` é mantido durante TODO o `await _loopTask`. `SemaphoreSlim` não tem afinidade de thread — é seguro fazer `await` com o semáforo adquirido. Isso serializa completamente as transições:

```
AdicionarConexao:
  await _cicloSemaphore.WaitAsync()      ← BLOQUEADO se PararLoopAsync está aguardando _loopTask
  try
    lock (_socketsLock) { _sockets.Add(socket) }
    if contador == 1 → IniciarColetaAsync()
  finally
    _cicloSemaphore.Release()

RemoverConexao (chamado de FORA do loop):
  await _cicloSemaphore.WaitAsync()
  try
    lock (_socketsLock) { _sockets.Remove(socket) }
    if contador == 0 → PararLoopAsync()   ← await _loopTask aqui, DENTRO do semáforo
  finally
    _cicloSemaphore.Release()
```

`PararLoopAsync` faz `await _loopTask` **dentro** do `_cicloSemaphore`. Enquanto esse `await` está em andamento, `AdicionarConexao` fica bloqueada no `WaitAsync`. Quando o loop antigo finalmente termina, o semáforo é liberado e `AdicionarConexao` pode prosseguir — iniciando um ciclo completamente novo.

**Cenário validado:**

```
Cliente A conecta    → semáforo adquirido → count 0→1 → IniciarColetaAsync → semáforo liberado
Cliente A desconecta → semáforo adquirido → count 1→0 → PararLoopAsync → await _loopTask (semáforo MANTIDO)
Cliente B tenta      → WaitAsync BLOQUEADO (semáforo ainda segurado pelo PararLoopAsync)
_loopTask termina    → PararLoopAsync termina → Dispose timer/CTS → semáforo liberado
Cliente B prossegue  → count 0→1 → IniciarColetaAsync → NOVO timer, NOVO CTS, NOVO _loopTask
```

#### Solução para self-await: dois métodos de remoção

O problema original: se `SendAsync` falha dentro de `LoopColetaAsync`, a chamada a `RemoverConexao` poderia disparar `PararLoopAsync` → `await _loopTask`, e `_loopTask` é a task que está executando o próprio `LoopColetaAsync` — self-await.

**Solução:** separar a remoção de socket em dois métodos com propósitos diferentes:

| Método | Chamado por | Comportamento |
|---|---|---|
| `RemoverSocketDoBroadcast(socket)` | `LoopColetaAsync` (dentro do loop) | Apenas remove da lista. Se `Count == 0`, **cancela** `_cts`. NUNCA faz `await _loopTask`. |
| `RemoverConexao(socket)` | `HandleWebSocket` (fora do loop) | Remove da lista. Se `Count == 0`, **cancela** `_cts` E faz `await _loopTask`. |

O cancelamento do `_cts` faz com que `WaitForNextTickAsync` lance `OperationCanceledException` — o loop captura e encerra. O próprio loop é responsável pela sua saída; nenhum código de dentro do loop aguarda a si mesmo.

**Fluxo quando `SendAsync` falha (de dentro do loop):**

```
LoopColetaAsync:
  foreach socket:
    try await SendAsync
    catch → RemoverSocketDoBroadcast(socket)
              → lock remove da lista
              → se Count == 0 → _cts.Cancel()
              → NÃO faz await _loopTask ← crítico
  // próximo tick: WaitForNextTickAsync lança OperationCanceledException
  // loop captura e encerra
```

**Fluxo quando cliente fecha o socket (de fora do loop):**

```
HandleWebSocket:
  while (await ReceiveAsync) { ... }
  finally:
    RemoverConexao(socket)
      → semáforo adquirido
      → lock remove da lista
      → se Count == 0 → _cts.Cancel() + await _loopTask ← seguro (fora do loop)
      → semáforo liberado
```

#### Estrutura completa dos métodos

**`HandleWebSocket(WebSocket socket)`** — público, chamado pelo controller:

```
public async Task HandleWebSocket(WebSocket socket)
{
    await AdicionarConexao(socket);
    try
    {
        var buffer = new byte[1024];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, default);
            if (result.MessageType == WebSocketMessageType.Close)
                break;
        }
    }
    finally
    {
        await RemoverConexao(socket);
    }
}
```

`ReceiveAsync` é assíncrono — o `await` libera a thread do ThreadPool enquanto o socket está ativo aguardando dados. A thread não fica bloqueada. Manter o método aguardando `ReceiveAsync` enquanto o WebSocket está conectado é intencional — é o padrão de WebSocket no ASP.NET Core.

**`AdicionarConexao(WebSocket socket)`** — privado:

```
private async Task AdicionarConexao(WebSocket socket)
{
    await _cicloSemaphore.WaitAsync();
    try
    {
        int count;
        lock (_socketsLock) { _sockets.Add(socket); count = _sockets.Count; }
        if (count == 1)
            IniciarColeta();
    }
    finally
    {
        _cicloSemaphore.Release();
    }
}
```

**`RemoverConexao(WebSocket socket)`** — privado, chamado somente de `HandleWebSocket`:

```
private async Task RemoverConexao(WebSocket socket)
{
    await _cicloSemaphore.WaitAsync();
    try
    {
        int count;
        lock (_socketsLock) { _sockets.Remove(socket); count = _sockets.Count; }
        if (count == 0)
            await PararLoopAsync();
    }
    finally
    {
        _cicloSemaphore.Release();
    }
}
```

**`RemoverSocketDoBroadcast(WebSocket socket)`** — privado, chamado somente de `LoopColetaAsync`:

```
private void RemoverSocketDoBroadcast(WebSocket socket)
{
    int count;
    lock (_socketsLock) { _sockets.Remove(socket); count = _sockets.Count; }
    if (count == 0)
        _cts?.Cancel();
    // NUNCA faz await _loopTask
}
```

**`IniciarColeta()`** — privado, chamado dentro do `_cicloSemaphore`:

```
private void IniciarColeta()
{
    _cts = new CancellationTokenSource();
    _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
    _loopTask = LoopColetaAsync(_cts.Token);
}
```

**`PararLoopAsync()`** — privado, chamado dentro do `_cicloSemaphore`:

```
private async Task PararLoopAsync()
{
    _cts?.Cancel();
    if (_loopTask is not null)
        await _loopTask;
    _timer?.Dispose();
    _cts?.Dispose();
    _timer = null;
    _cts = null;
    _loopTask = null;
}
```

**`LoopColetaAsync(CancellationToken ct)`** — privado:

```
private async Task LoopColetaAsync(CancellationToken ct)
{
    try
    {
        while (await _timer!.WaitForNextTickAsync(ct))
        {
            var snapshotBase = await _coletor.ColetarAsync(ct);

            List<WebSocket> socketsCopia;
            int clientes;
            int numero;
            lock (_socketsLock)
            {
                socketsCopia = new List<WebSocket>(_sockets);
                clientes = _sockets.Count;
                numero = ++_numeroSnapshots;
            }

            var snapshot = snapshotBase with
            {
                ClientesConectados = clientes,
                ContadorSnapshots = numero
            };

            var json = JsonSerializer.Serialize(snapshot);
            var buffer = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(buffer);

            for (int i = socketsCopia.Count - 1; i >= 0; i--)
            {
                var ws = socketsCopia[i];
                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await ws.SendAsync(segment, WebSocketMessageType.Text, true, timeoutCts.Token);
                }
                catch
                {
                    RemoverSocketDoBroadcast(ws);
                }
            }
        }
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        // Loop encerrado por cancelamento do _cts — esperado
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Loop de coleta encerrado por exceção inesperada");
        // _cts é cancelado para sinalizar que o loop morreu;
        // clientes existentes serão removidos quando seus ReceiveAsync falharem
        _cts?.Cancel();
    }
}
```

**Tratamento de exceção inesperada:** se `ColetarAsync` ou qualquer operação dentro do loop lançar algo que não seja `OperationCanceledException`, o loop loga o erro e se encerra. Os WebSockets conectados continuam vivos (o `HandleWebSocket` de cada um ainda está no loop de `ReceiveAsync`), mas param de receber snapshots. Na prática, se o loop morrer, os clientes perceberão ausência de atualizações e eventualmente o frontend indicará "desconectado" por timeout. Um novo cliente que conectar depois disparará um novo ciclo (o `SemaphoreSlim` estará livre — o loop antigo já encerrou).

#### Broadcast sequencial (sem paralelismo)

O loop itera os sockets em sequência com `await SendAsync`. **Regra: no máximo UMA operação `SendAsync` por socket em andamento.** Cada envio tem timeout de 2s via `CancellationTokenSource`. Se falhar, `RemoverSocketDoBroadcast` remove o socket da lista (e pode cancelar o `_cts` se for o último cliente).

#### Tratamento de desconexão abrupta

- `ReceiveAsync` lança `WebSocketException` ou retorna `CloseStatus` → `finally` chama `RemoverConexao` (fora do loop, seguro)
- `SendAsync` falha no broadcast → `RemoverSocketDoBroadcast` (dentro do loop, seguro — no máximo cancela `_cts`)
- `RemoverSocketDoBroadcast` e `RemoverConexao` são idempotentes — se o socket já foi removido, `Remove` não faz nada, `Count` não muda, e a transição não dispara

### 1.4 — Backend: MonitoramentoController

#### `backend/src/Controllers/MonitoramentoController.cs` (novo)

```csharp
[ApiController]
[Route("api/monitoramento")]
public class MonitoramentoController(MonitoramentoService monitoramentoService) : ControllerBase
{
    [HttpGet("ws")]
    public async Task<IActionResult> WebSocket()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return BadRequest("WebSocket esperado");

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await monitoramentoService.HandleWebSocket(webSocket);
        return Empty;
    }
}
```

`HandleWebSocket` só retorna quando o socket fecha. O controller retorna `Empty` — a resposta HTTP não tem corpo relevante para WebSocket.

### 1.5 — Backend: Registro no Program.cs

**Arquivo modificado:** `backend/Program.cs`

Inserir após `builder.Services.AddSingleton<MigrationService>()`:

```csharp
builder.Services.AddSingleton<MonitoramentoService>();
builder.Services.AddSingleton<IColetorMetricas, MetricasTesteColetor>();
```

Inserir `app.UseWebSockets()` **entre** `UseCors` e `UseHttpsRedirection`:

```csharp
app.UseCors("AllowAll");
app.UseWebSockets();                    // ← NOVO
app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapControllers();
```

**Nada mais muda.**

### 1.6 — Frontend: Tipo e model

#### `frontend/src/types/index.ts` — adicionar:

```ts
export interface IMonitoramentoSnapshot {
  timestamp: string
  plataforma: string
  clientesConectados: number
  contadorSnapshots: number
  mensagem: string
}
```

#### `frontend/src/models/MonitoramentoModel.ts` (novo)

```ts
import type { IMonitoramentoSnapshot } from '@/types'

export default class MonitoramentoModel implements IMonitoramentoSnapshot {
  timestamp: string
  plataforma: string
  clientesConectados: number
  contadorSnapshots: number
  mensagem: string

  constructor(obj: Partial<IMonitoramentoSnapshot> = {}) {
    this.timestamp = obj.timestamp || new Date().toISOString()
    this.plataforma = obj.plataforma || ''
    this.clientesConectados = obj.clientesConectados || 0
    this.contadorSnapshots = obj.contadorSnapshots || 0
    this.mensagem = obj.mensagem || ''
  }
}
```

### 1.7 — Frontend: Service

#### `frontend/src/services/monitoramentoService.ts` (novo)

Wrapper do WebSocket nativo. **Não estende `BaseApiService`** — WebSocket não é HTTP.

#### URL do WebSocket

```ts
class MonitoramentoService {
  private socket: WebSocket | null = null
  private url: string
  private timerReconexao: ReturnType<typeof setTimeout> | null = null
  private tentativas: number = 0
  private desconexaoIntencional: boolean = false
  private onMessageCallback: ((data: IMonitoramentoSnapshot) => void) | null = null
  private onStatusCallback: ((conectado: boolean) => void) | null = null

  constructor() {
    const isDev = import.meta.env.DEV
    const host = isDev ? 'localhost:2024' : location.host
    const protocol = location.protocol === 'https:' ? 'wss' : 'ws'
    this.url = `${protocol}://${host}/api/monitoramento/ws`
  }
}
```

| Ambiente | Host | URL resultante |
|---|---|---|
| Dev (Vite) | `localhost:2024` | `ws://localhost:2024/api/monitoramento/ws` |
| Prod, notebook (localhost) | `localhost:2025` | `ws://localhost:2025/api/monitoramento/ws` |
| Prod, notebook (IP) | `192.168.0.10:2025` | `ws://192.168.0.10:2025/api/monitoramento/ws` |
| Prod, celular | `192.168.0.10:2025` | `ws://192.168.0.10:2025/api/monitoramento/ws` |
| Prod HTTPS (futuro) | `host` | `wss://host/api/monitoramento/ws` |

#### Métodos

**`conectar(onMessage, onStatusChange): void`**

1. `desconexaoIntencional = false`
2. Armazena callbacks
3. Cria `new WebSocket(this.url)`
4. `onopen`: `tentativas = 0`; `onStatusChange(true)`
5. `onmessage`: parse JSON → `new MonitoramentoModel(...)` → `onMessage`
6. `onclose`: chama `tratarFechamento()`
7. `onerror`: **NÃO agenda reconexão** — `onclose` sempre dispara depois de `onerror`, evita duplicidade

**`desconectar(): void`**

1. `desconexaoIntencional = true`
2. `clearTimeout(timerReconexao)`
3. `socket?.close(1000, 'desconexao manual')`
4. `socket = null`

**`tratarFechamento(): void`** (privado)

1. `onStatusChange(false)`
2. Se `desconexaoIntencional` → retorna sem reconectar
3. Chama `agendarReconexao()`

**`agendarReconexao(): void`** (privado)

1. `clearTimeout(timerReconexao)` — cancela qualquer timer pendente
2. Delay: `Math.min(2000 * Math.pow(2, tentativas), 30000)`
3. `tentativas++`
4. `timerReconexao = setTimeout(() => this.conectar(...callbacks...), delay)`

### 1.8 — Frontend: Store

#### `frontend/src/stores/monitoramento.ts` (novo)

Pinia Options API:

```ts
export const useMonitoramentoStore = defineStore('monitoramento', {
  state: (): MonitoramentoState => ({
    snapshot: null as IMonitoramentoSnapshot | null,
    conectado: false,
    ultimaAtualizacao: null as Date | null,
    erro: null as string | null
  }),
  getters: {
    plataforma: (state) => state.snapshot?.plataforma ?? '',
    clientesConectados: (state) => state.snapshot?.clientesConectados ?? 0,
    tempoDesdeUltimaAtualizacao: (state) =>
      state.ultimaAtualizacao
        ? Math.round((Date.now() - state.ultimaAtualizacao.getTime()) / 1000)
        : 0
  },
  actions: {
    conectar() {
      if (this._service) return      // proteção contra conexão dupla
      const service = new MonitoramentoService()
      service.conectar(
        (data: IMonitoramentoSnapshot) => {
          this.snapshot = data
          this.ultimaAtualizacao = new Date()
          this.erro = null
        },
        (status: boolean) => {
          this.conectado = status
          if (!status) this.snapshot = null
        }
      )
      this._service = service
    },
    desconectar() {
      this._service?.desconectar()
      this._service = null
      this.conectado = false
      this.snapshot = null
    }
  }
})
```

A view chama `conectar()` no `onMounted` e `desconectar()` no `onBeforeUnmount`.

### 1.9 — Frontend: View e rota

#### `frontend/src/views/MonitoramentoView.vue` (novo)

**Template** — cards Vuetify:

1. **Estado da conexão:** `v-chip` verde (`"Conectado"`) / vermelho (`"Desconectado"`), ícone `mdi-lan-connect` / `mdi-lan-disconnect`
2. **Plataforma:** ícone `mdi-microsoft-windows` ou `mdi-linux` + texto
3. **Timestamp:** formatado `HH:mm:ss` via `computed`
4. **Tempo desde última atualização:** "há Xs" — `text-warning` se `> 3`
5. **Clientes conectados:** número + `mdi-account-multiple`
6. **Indicador de tempo real:** animação de pulso CSS a cada novo `contadorSnapshots` (`watch`)
7. **Mensagem de teste:** conteúdo do campo `mensagem`

**Script setup:**
- `onMounted`: `store.conectar()`, inicia `setInterval` de 1s para o `ref agora`
- `onBeforeUnmount`: `store.desconectar()`, `clearInterval(timerAgora)`
- `const agora = ref(Date.now())` — atualizado a cada 1s via `setInterval`. **Necessário porque `Date.now()` não é reativo no Vue.** Se o getter da store dependesse só de `Date.now()`, o contador congelaria quando snapshots parassem de chegar.
- `const tempoDesdeUltimaAtualizacao = computed(...)` — calcula delta usando `agora.value` e `store.ultimaAtualizacao`
- `watch` no `contadorSnapshots` para animação de tempo real
- `computed` para formatação de timestamp

**Estilo:** `v-container` + `v-row`/`v-col`, cards `v-card`, responsivo (12 cols mobile).

#### `frontend/src/router/index.ts` — adicionar:

```ts
{
  path: '/monitoramento',
  name: 'monitoramento',
  component: () => import('../views/MonitoramentoView.vue')
}
```

### 1.10 — Testes

#### Testes de integração (prioridade)

**`backend/tests/ProjectManagerWeb.Tests/MonitoramentoWebSocketTests.cs`** (novo)

Usa `WebApplicationFactory<Program>` + `ClientWebSocket`. Este é o teste principal — valida comportamento real do WebSocket sem expor estado interno.

| Teste | O que verifica |
|---|---|
| `Deve_receber_snapshot_apos_conectar` | Conecta, recebe mensagem em 5s, parse válido |
| `Deve_parar_coleta_quando_ultimo_cliente_desconecta` | Conecta, fecha, aguarda 2s, reconecta — `ContadorSnapshots` recomeçou (prova que loop anterior parou e novo iniciou) |
| `Deve_incrementar_contador_clientes` | 2 clientes simultâneos → `ClientesConectados == 2` |
| `Deve_manter_loop_com_multiplos_clientes` | 2 clientes, desconecta 1 → snapshots continuam, `ClientesConectados == 1` |
| `Deve_tratar_desconexao_abrupta_sem_quebrar_servidor` | Fecha socket sem handshake de close → servidor não quebra, outros clientes continuam recebendo |

#### Testes unitários (complementares)

**`backend/tests/ProjectManagerWeb.Tests/Services/MonitoramentoServiceTests.cs`** (novo)

Testam a **lógica de transição de estado** do `MonitoramentoService`. Usam NSubstitute para mock de `WebSocket` (classe abstrata — `Substitute.For<WebSocket>()`) e `IColetorMetricas`.

**Estado NÃO exposto publicamente.** Os testes validam comportamento indireto via chamadas ao coletor:

| Teste | Como verifica |
|---|---|
| Primeiro cliente inicia loop | `AdicionarConexao` → `_coletor.Received().ColetarAsync(...)` após aguardar 1 tick |
| Segundo cliente não inicia outro loop | 2 sockets → coletor chamado 1x por tick (contagem de chamadas) |
| Remoção mantém loop com 1 cliente restante | Remove socket 2 → coletor continua sendo chamado |
| Último cliente para o loop | Remove socket 1 → `_coletor.DidNotReceive().ColetarAsync(...)` após aguardar parada |
| Remoção duplicada não quebra | Remove mesmo socket 2x → sem exceção |
| Reconexão imediata (race condition) | Remove socket 1 → `await` estabilização → adiciona socket 2 → coletor volta a ser chamado (loop novo) |
| `RemoverSocketDoBroadcast` não causa self-await | Simula remoção de dentro do contexto do loop — sem exceção, sem deadlock |
| `SendAsync` falhando remove socket sem parar loop se outros existem | 2 sockets mock: 1 falha `SendAsync`, 1 ok → loop continua, socket falho removido |
| `SendAsync` falha no último socket + novo cliente reconecta antes do loop encerrar | Último socket falha → `_cts.Cancel()` → novo socket adicionado → `SemaphoreSlim` bloqueia até loop antigo terminar → novo ciclo inicia corretamente |

#### Testes de integração — cenário de race condition no broadcast

Adicionar ao `MonitoramentoWebSocketTests`:

| Teste | O que verifica |
|---|---|
| `Deve_iniciar_novo_ciclo_apos_broadcast_remover_ultimo_socket_e_novo_cliente_conectar` | Conecta 1 cliente → força falha do `SendAsync` (fecha socket abruptamente) → loop cancela `_cts` → **imediatamente** conecta novo cliente → verifica que snapshots voltam a ser recebidos (prova que loop antigo encerrou e novo ciclo iniciou corretamente, sem deadlock nem dois loops) |

Este é o teste que cobre o cenário mais sutil de concorrência: quando o `_cts.Cancel()` parte de dentro do loop (via `RemoverSocketDoBroadcast`) e um novo cliente chega quase simultaneamente. O `SemaphoreSlim` garante a serialização, mas o teste prova que ela funciona no contexto real do `LoopColetaAsync`.

### 1.11 — Validação manual

1. **Notebook local (dev):**
   - `dotnet run` → `http://localhost:2024/monitoramento`
   - Conexão verde, snapshots ~1s, campos visíveis
   - Segunda aba → contador = 2
   - Fecha uma aba → contador = 1
   - Fecha última aba → reabre → contador = 1, snapshots voltam (prova: loop parou e reiniciou)
   - `Ctrl+C` backend → "Desconectado" → reinicia backend → reconexão em ~2s

2. **Rede local — celular:**
   - Confirma IP do notebook
   - Chrome Android: `http://192.168.x.x:2025/monitoramento`
   - Mesmos testes acima
   - Wi-Fi off/on → reconexão automática

---

## Fase 2 — CPU e RAM

**Objetivo:** coleta real de uso de CPU e memória RAM em Windows e Linux, sem dependências externas.

### 2.1 — Backend: Expansão do DTO

#### `backend/src/DTOs/MonitoramentoSnapshotDTO.cs` — novo record:

```csharp
public sealed record MonitoramentoSnapshotDTO(
    DateTime Timestamp,
    string Plataforma,
    int ClientesConectados,
    int ContadorSnapshots,
    string Mensagem,
    double? CpuPercentual,
    long? RamTotalBytes,
    long? RamDisponivelBytes,
    long? RamUsadaBytes
)
```

Novos campos são `null` para coletores que não os preenchem. O frontend renderiza condicionalmente.

### 2.2 — Backend: Coletor cross-platform

```
backend/src/Services/Monitoramento/Coletores/
├── ICpuRamColetor.cs          (interface interna)
├── CpuRamColetor.cs           (fachada cross-platform, implementa IColetorMetricas)
├── WindowsCpuRamColetor.cs    (P/Invoke kernel32.dll)
└── LinuxCpuRamColetor.cs      (leitura de /proc)
```

#### `ICpuRamColetor.cs`

```csharp
internal interface ICpuRamColetor
{
    double? ObterCpuPercentual();
    (long total, long disponivel) ObterMemoria();
}
```

Métodos síncronos — leitura de arquivo ou P/Invoke é instantânea.

#### `CpuRamColetor.cs`

Implementa `IColetorMetricas`. Construtor recebe `ICpuRamColetor`.

```
ColetarAsync(ct):
  cpu = _cpuImpl.ObterCpuPercentual()
  (total, disponivel) = _cpuImpl.ObterMemoria()
  usado = total - disponivel

  return new MonitoramentoSnapshotDTO(
      Timestamp: DateTime.UtcNow,
      Plataforma: ...,
      ClientesConectados: 0,
      ContadorSnapshots: 0,
      Mensagem: "",
      CpuPercentual: cpu,
      RamTotalBytes: total > 0 ? total : null,
      RamDisponivelBytes: total > 0 ? disponivel : null,
      RamUsadaBytes: total > 0 ? usado : null
  )
```

#### `WindowsCpuRamColetor.cs`

**Zero dependências** — `kernel32.dll` é parte do Windows.

**CPU:** P/Invoke `GetSystemTimes`:

```csharp
[DllImport("kernel32.dll")]
private static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);
```

- Mantém estado: `_idleAnterior`, `_kernelAnterior`, `_userAnterior`
- Primeira chamada: armazena valores, retorna `null`
- Chamadas seguintes: `totalDelta = deltaKernel + deltaUser`; `cpu = (1.0 - deltaIdle / totalDelta) * 100.0`
- Se `totalDelta == 0`, retorna `null`

**RAM:** P/Invoke `GlobalMemoryStatusEx`:

```csharp
[StructLayout(LayoutKind.Sequential)]
private struct MEMORYSTATUSEX { ... }

[DllImport("kernel32.dll")]
private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
```

- `dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()` antes da chamada
- Retorna `(long)status.ullTotalPhys, (long)status.ullAvailPhys`

#### `LinuxCpuRamColetor.cs`

**Zero dependências** — `/proc` é parte do kernel Linux.

**CPU — `/proc/stat`:**

Formato da primeira linha:
```
cpu  user nice system idle iowait irq softirq steal guest guest_nice
```

`guest` e `guest_nice` já estão contabilizados em `user` e `nice` (kernel >= 2.6.33). **Não** são somados separadamente.

**Fórmula de percentual de CPU:**

```
totalAtivo = user + nice + system + irq + softirq + steal
totalIdle = idle + iowait
total = totalAtivo + totalIdle

deltaTotal = total - totalAnterior
deltaIdle = totalIdle - totalIdleAnterior

cpuPercentual = (1.0 - deltaIdle / deltaTotal) * 100.0
```

- `iowait` é incluído em `idle` (convenção padrão: `top`, `htop`). I/O wait não conta como "CPU ocupada".
- Se `deltaTotal == 0`, retorna `null`.
- Primeira chamada: armazena baseline, retorna `null`.

Estado entre chamadas: `_userAnterior`, `_niceAnterior`, `_systemAnterior`, `_idleAnterior`, `_iowaitAnterior`, `_irqAnterior`, `_softirqAnterior`, `_stealAnterior`.

**RAM — `/proc/meminfo`:**

| Campo | Regex |
|---|---|
| MemTotal | `MemTotal:\s+(\d+)` |
| MemAvailable | `MemAvailable:\s+(\d+)` |

Valores em kB → `* 1024` para bytes. Falha → `(0, 0)`.

**Construtor de teste:**

```csharp
internal LinuxCpuRamColetor(string caminhoStat, string caminhoMeminfo) : this()
{
    _caminhoStat = caminhoStat;
    _caminhoMeminfo = caminhoMeminfo;
}
```

### 2.3 — Backend: Registro no DI

```csharp
// Remove MetricasTesteColetor

if (OperatingSystem.IsWindows())
    builder.Services.AddSingleton<ICpuRamColetor, WindowsCpuRamColetor>();
else
    builder.Services.AddSingleton<ICpuRamColetor, LinuxCpuRamColetor>();

builder.Services.AddSingleton<IColetorMetricas, CpuRamColetor>();
```

O `MonitoramentoService` não muda — recebe `IColetorMetricas` via DI.

### 2.4 — Frontend

**Types:** adicionar `cpuPercentual?`, `ramTotalBytes?`, `ramDisponivelBytes?`, `ramUsadaBytes?` como `number | null`.

**Model:** adicionar os 4 campos com default `null`.

**View:** cards de CPU (`v-progress-linear` com threshold de cor) e RAM (barra + texto "X.X GB / Y.Y GB"), renderizados condicionalmente (`!== null`).

### 2.5 — Testes

**`LinuxCpuRamColetorTests.cs`:** fixtures `/proc` via construtor `internal`, testa primeira chamada `null`, segunda com percentual, delta zero, MemTotal/Available, arquivo inexistente, parse resiliente.

**`CpuRamColetorTests.cs`:** mock `ICpuRamColetor` (NSubstitute), testa delegação de CPU e RAM, verifica que campos de transporte não são preenchidos.

### 2.6 — Validação manual

1. **Windows:** CPU ~5-20% idle, RAM plausível, abre app pesada → CPU sobe, reabre aba → primeira leitura `null`.
2. **Linux:** mesmos testes, `/proc` funcionando.

---

## Fases Futuras — Direção Arquitetural

### Como adicionar uma nova métrica

1. Criar interface específica se houver variação por plataforma (`IDiscoColetor`)
2. Implementações por plataforma (`WindowsDiscoColetor`, `LinuxDiscoColetor`)
3. Fachada `DiscoColetor : IColetorMetricas`
4. Expandir `MonitoramentoSnapshotDTO` com campos `double?`/`long?`
5. Para múltiplos coletores simultâneos: `ColetorComposto : IColetorMetricas` recebe `IEnumerable<IColetorMetricas>`, chama cada um, faz merge com `with`
6. `MonitoramentoService` continua inalterado

### Métricas planejadas

| Métrica | Windows (API) | Linux (fonte) | Campo DTO |
|---|---|---|---|
| **Disco** | `GetDiskFreeSpaceEx` + Performance Counters | `/proc/diskstats` + `statfs` | `DiscoPercentual`, `DiscoLeituraBytes`, `DiscoEscritaBytes` |
| **Rede** | `GetIfTable2` / `GetIpStatisticsEx` (iphlpapi.dll) | `/proc/net/dev` | `RedeUploadBytes`, `RedeDownloadBytes` |
| **GPU** | NVML, DXGI, WMI | NVML, `/sys/class/drm` | `GpuPercentual`, `GpuTemperatura`, `VramUsadaBytes` |
| **Temperatura CPU** | WMI / MSAcpi_ThermalZoneTemperature | `/sys/class/thermal/thermal_zone*/temp` | `CpuTemperatura` |
| **Temperatura discos** | WMI `MSStorageDriver_ATAPISmartData` | `smartctl` / `/sys/block/*/device/hwmon` | Campos por disco |
| **Bateria** | `GetSystemPowerStatus` (kernel32) | `/sys/class/power_supply/BAT0/capacity` | `BateriaPercentual`, `BateriaCarregando` |
| **Processos** | `CreateToolhelp32Snapshot` (kernel32) | `/proc` scan | `TotalProcessos`, `TopProcessos[]` |
| **Fans** | WMI / HWMonitor SDK | `/sys/class/hwmon` | `VelocidadeFansRpm[]` |
| **Load Average** | Performance Counter `\System\Processor Queue Length` | `/proc/loadavg` | `LoadAverage1m`, `LoadAverage5m`, `LoadAverage15m` |
| **Uptime** | `GetTickCount64` / `Environment.TickCount64` | `/proc/uptime` | `UptimeSegundos` |

### Princípios

- Cada métrica = um coletor independente (`IColetorMetricas`)
- Composição via `ColetorComposto`, sem alterar `MonitoramentoService`
- Campos novos são `nullable` — coletor que não suporta simplesmente não preenche
- Zero dependências NuGet / npm em todas as fases
- **Frequência de coleta é responsabilidade do ColetorComposto, não do `MonitoramentoService`.** O `MonitoramentoService` só sabe "tem cliente? peço snapshot a cada 1s". Se futuramente algumas métricas precisarem de frequência menor (ex.: temperatura a cada 5s), o `ColetorComposto` internamente decide quais coletores chamar a cada tick (cacheando valores entre ticks ou usando timers internos próprios). O transporte permanece agnóstico a isso.
- O `MonitoramentoService` nunca cresce para um "monstro" de 1500 linhas — a expansão é sempre via novos coletores plugados no `ColetorComposto`

---

## Checkpoints de implementação

A implementação deve ser feita em checkpoints pequenos, com testes após cada etapa. **Não implementar tudo de uma vez.**

### Checkpoint 1 — Backend (sem frontend)

- `MonitoramentoSnapshotDTO`
- `IColetorMetricas`
- `MetricasTesteColetor`
- `MonitoramentoService`
- `MonitoramentoController`
- Registro no `Program.cs` (DI + `UseWebSockets`)
- **Testes unitários + integração**
- **Rodar testes** (`dotnet test`)

### Checkpoint 2 — Frontend

- `IMonitoramentoSnapshot` em `types/index.ts`
- `MonitoramentoModel`
- `monitoramentoService` (wrapper WebSocket)
- `monitoramentoStore`
- `MonitoramentoView.vue`
- Rota em `router/index.ts`
- **Build** (`pnpm run build`)
- **Testes manuais:** abrir `http://localhost:2024/monitoramento`

### Checkpoint 3 — Validação de comportamento

- Múltiplas abas → contador correto
- Desconexão → reconexão automática
- Fechar última aba → loop para; reabrir → loop inicia novo
- Parar backend → frontend mostra "Desconectado"; reiniciar → reconecta
- **Acesso LAN:** celular na mesma Wi-Fi

### Checkpoint 4 — Fase 2 (CPU e RAM)

- `ICpuRamColetor`
- `CpuRamColetor` (fachada)
- `WindowsCpuRamColetor`
- `LinuxCpuRamColetor`
- Expandir DTO e frontend
- **Testes unitários**
- **Validação manual Windows + Linux**

---

## Resumo de arquivos

### Fase 1 — Arquivos novos (11)

| Arquivo | Tipo |
|---|---|
| `backend/src/DTOs/MonitoramentoSnapshotDTO.cs` | record DTO |
| `backend/src/Services/Monitoramento/IColetorMetricas.cs` | interface |
| `backend/src/Services/Monitoramento/Coletores/MetricasTesteColetor.cs` | coletor de teste |
| `backend/src/Services/Monitoramento/MonitoramentoService.cs` | serviço singleton |
| `backend/src/Controllers/MonitoramentoController.cs` | controller WebSocket |
| `backend/tests/ProjectManagerWeb.Tests/Services/MonitoramentoServiceTests.cs` | testes unitários |
| `backend/tests/ProjectManagerWeb.Tests/MonitoramentoWebSocketTests.cs` | testes de integração |
| `frontend/src/models/MonitoramentoModel.ts` | model |
| `frontend/src/services/monitoramentoService.ts` | wrapper WebSocket |
| `frontend/src/stores/monitoramento.ts` | store Pinia |
| `frontend/src/views/MonitoramentoView.vue` | view |

### Fase 1 — Arquivos modificados (3)

| Arquivo | Alteração |
|---|---|
| `backend/Program.cs` | +3 linhas DI, +1 linha `UseWebSockets` |
| `frontend/src/router/index.ts` | +1 rota |
| `frontend/src/types/index.ts` | +1 interface |

### Fase 2 — Arquivos novos (6)

| Arquivo | Tipo |
|---|---|
| `backend/src/Services/Monitoramento/Coletores/ICpuRamColetor.cs` | interface interna |
| `backend/src/Services/Monitoramento/Coletores/CpuRamColetor.cs` | fachada cross-platform |
| `backend/src/Services/Monitoramento/Coletores/WindowsCpuRamColetor.cs` | P/Invoke Windows |
| `backend/src/Services/Monitoramento/Coletores/LinuxCpuRamColetor.cs` | /proc Linux |
| `backend/tests/ProjectManagerWeb.Tests/Services/Monitoramento/LinuxCpuRamColetorTests.cs` | testes |
| `backend/tests/ProjectManagerWeb.Tests/Services/Monitoramento/CpuRamColetorTests.cs` | testes |

### Fase 2 — Arquivos modificados (5)

| Arquivo | Alteração |
|---|---|
| `backend/Program.cs` | trocar registro do coletor + registro por OS |
| `backend/src/DTOs/MonitoramentoSnapshotDTO.cs` | + campos CPU/RAM |
| `frontend/src/types/index.ts` | + campos CPU/RAM |
| `frontend/src/models/MonitoramentoModel.ts` | + campos CPU/RAM |
| `frontend/src/views/MonitoramentoView.vue` | + cards de CPU/RAM |

### Dependências

**Zero dependências novas em ambas as fases.**

---

## Checklist de riscos resolvidos

| Risco | Solução |
|---|---|
| **Race condition: `PararLoopAsync` libera lock e `AdicionarConexao` inicia novo loop antes do antigo terminar** | `SemaphoreSlim(1,1)` mantido durante `await _loopTask`; `AdicionarConexao` bloqueada até loop anterior completamente encerrado (timer/CTS descartados, `_loopTask = null`) |
| **Self-await: `SendAsync` falha → `RemoverConexao` → `await _loopTask` dentro do próprio `LoopColetaAsync`** | Dois métodos de remoção: `RemoverSocketDoBroadcast` (chamado de dentro do loop, apenas cancela `_cts`, nunca faz `await`) e `RemoverConexao` (chamado de fora do loop, faz `await _loopTask` seguro). O loop encerra a si mesmo via `OperationCanceledException`. |
| **Dois loops simultâneos** | `SemaphoreSlim` serializa completamente transições 0↔1; novo ciclo sempre cria instâncias novas de timer/CTS/loopTask |
| **CTS reutilizado após cancelamento** | `PararLoopAsync` descarta `_cts`/`_timer` e seta para `null`; `IniciarColeta` sempre cria novos |
| **Acesso LAN: bind apenas em localhost** | Verificado: `app.Urls.Add("http://*:{porta}")` → bind `0.0.0.0`; acessível por IP na rede |
| **`UseHttpsRedirection` quebrando WebSocket HTTP** | Sem porta HTTPS configurada, middleware não redireciona; `ws://` funciona normalmente |
| **Dupla reconexão (`onerror` + `onclose`)** | `onerror` não agenda; só `onclose` chama `tratarFechamento`; `agendarReconexao` cancela timer pendente |
| **`desconectar()` seguido de reconexão** | Flag `desconexaoIntencional`; `tratarFechamento` verifica antes de reconectar |
| **Contador/testabilidade via estado público** | Estado privado; testes unitários via NSubstitute (chamadas ao coletor); testes de integração via `WebApplicationFactory` + `ClientWebSocket` |
| **Coletor dependente de transporte** | Coletor retorna métricas puras; serviço preenche `ClientesConectados`/`ContadorSnapshots` via `with` |
| **Timer alocado sem clientes** | Timer criado em `IniciarColeta`, descartado em `PararLoopAsync`, `null` quando parado |
| **Cálculo CPU Linux ambíguo** | Fórmula documentada: `(1 - deltaIdle/deltaTotal) * 100`; `idle` inclui `iowait`; `guest`/`guest_nice` não duplicados |
| **Broadcast com `SendAsync` travando loop** | Timeout individual de 2s por socket; falha remove socket específico e continua |
| **Loop morrendo silenciosamente por exceção inesperada** | `catch (Exception)` com `ILogger`; loga erro e cancela `_cts` para encerrar ciclo; novo cliente inicia ciclo limpo |
| **Race condition: `_cts.Cancel()` de dentro do loop + novo cliente simultâneo** | `SemaphoreSlim` bloqueia `AdicionarConexao` até loop antigo terminar; teste de integração dedicado cobre o cenário |
| **`conectar()` duplo no frontend criando múltiplos WebSockets** | Guard clause `if (this._service) return` na store |
| **`tempoDesdeUltimaAtualizacao` congelado quando snapshots param** | `ref` com `setInterval` de 1s na view; `Date.now()` não é reativo — o `ref` fornece a reatividade necessária |

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento;

public class MonitoramentoService(IColetorMetricas coletor, ILogger<MonitoramentoService> logger)
{
    private static readonly TimeSpan IntervaloColeta = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan TimeoutEnvio = TimeSpan.FromSeconds(2);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly SemaphoreSlim _cicloSemaphore = new(1, 1);
    private readonly object _socketsLock = new();
    private readonly List<WebSocket> _sockets = [];

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private int _numeroSnapshots;

    public async Task HandleWebSocketAsync(WebSocket socket)
    {
        await AdicionarConexaoAsync(socket);
        try
        {
            var buffer = new byte[1024];
            while (socket.State == WebSocketState.Open)
            {
                var resultado = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (resultado.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Encerrado pelo servidor", CancellationToken.None);
                    break;
                }
            }
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            await RemoverConexaoAsync(socket);
        }
    }

    private async Task AdicionarConexaoAsync(WebSocket socket)
    {
        await _cicloSemaphore.WaitAsync();
        try
        {
            int quantidade;
            lock (_socketsLock)
            {
                _sockets.Add(socket);
                quantidade = _sockets.Count;
            }

            if (quantidade == 1)
                IniciarColeta();
        }
        finally
        {
            _cicloSemaphore.Release();
        }
    }

    private async Task RemoverConexaoAsync(WebSocket socket)
    {
        await _cicloSemaphore.WaitAsync();
        try
        {
            int quantidade;
            lock (_socketsLock)
            {
                _sockets.Remove(socket);
                quantidade = _sockets.Count;
            }

            if (quantidade == 0)
                await PararLoopAsync();
        }
        finally
        {
            _cicloSemaphore.Release();
        }
    }

    private void RemoverSocketDoBroadcast(WebSocket socket)
    {
        AbortarSilenciosamente(socket);

        int quantidade;
        lock (_socketsLock)
        {
            _sockets.Remove(socket);
            quantidade = _sockets.Count;
        }

        if (quantidade == 0)
            _cts?.Cancel();
    }

    private void IniciarColeta()
    {
        _timer?.Dispose();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(IntervaloColeta);
        _loopTask = LoopColetaAsync(_cts.Token);
    }

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

    private async Task LoopColetaAsync(CancellationToken ct)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(ct))
            {
                var snapshotBase = await coletor.ColetarAsync(ct);

                List<WebSocket> socketsCopia;
                int clientes;
                int numero;
                lock (_socketsLock)
                {
                    socketsCopia = [.. _sockets];
                    clientes = _sockets.Count;
                    numero = ++_numeroSnapshots;
                }

                var snapshot = snapshotBase with
                {
                    ClientesConectados = clientes,
                    ContadorSnapshots = numero
                };

                var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot, JsonOptions));
                var segmento = new ArraySegment<byte>(buffer);

                for (int i = socketsCopia.Count - 1; i >= 0; i--)
                {
                    var ws = socketsCopia[i];
                    try
                    {
                        using var timeoutCts = new CancellationTokenSource(TimeoutEnvio);
                        await ws.SendAsync(segmento, WebSocketMessageType.Text, true, timeoutCts.Token);
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Loop de coleta encerrado por exceção inesperada");
            _cts?.Cancel();
        }
    }

    private static void AbortarSilenciosamente(WebSocket socket)
    {
        try
        {
            socket.Abort();
        }
        catch
        {
        }
    }
}

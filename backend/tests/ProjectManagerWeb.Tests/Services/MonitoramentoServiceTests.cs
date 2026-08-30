using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services.Monitoramento;

namespace ProjectManagerWeb.Tests.Services;

public class MonitoramentoServiceTests
{
    private readonly IColetorMetricas _coletor = Substitute.For<IColetorMetricas>();
    private readonly ILogger<MonitoramentoService> _logger = Substitute.For<ILogger<MonitoramentoService>>();
    private readonly MonitoramentoService _sut;

    public MonitoramentoServiceTests()
    {
        _coletor.ColetarAsync(Arg.Any<CancellationToken>())
            .Returns(new MonitoramentoSnapshotDTO(DateTime.UtcNow, "linux", 0, 0, "Linux", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null));
        _sut = new MonitoramentoService(_coletor, _logger);
    }

    private static WebSocket CriarSocketQueRespondeClose()
    {
        var socket = Substitute.For<WebSocket>();
        socket.State.Returns(WebSocketState.Open);
        socket.ReceiveAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
            .Returns(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, ""));
        socket.CloseAsync(Arg.Any<WebSocketCloseStatus>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return socket;
    }

    public class HandleWebSocketAsync : MonitoramentoServiceTests
    {
        [Fact]
        public async Task Deve_encerrar_sem_excecao_quando_recebe_close_no_primeiro_receive()
        {
            var socket = CriarSocketQueRespondeClose();

            var act = () => _sut.HandleWebSocketAsync(socket);

            await act.Should().NotThrowAsync();
            await socket.Received(1).CloseAsync(WebSocketCloseStatus.NormalClosure, "Encerrado pelo servidor", CancellationToken.None);
        }

        [Fact]
        public async Task Deve_encerrar_sem_excecao_quando_receive_lanca_websocket_exception()
        {
            var socket = Substitute.For<WebSocket>();
            socket.State.Returns(WebSocketState.Open);
            socket.ReceiveAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new WebSocketException(WebSocketError.ConnectionClosedPrematurely));

            var act = () => _sut.HandleWebSocketAsync(socket);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Deve_ignorar_mensagem_de_texto_e_encerrar_no_close_seguinte()
        {
            var socket = Substitute.For<WebSocket>();
            socket.State.Returns(WebSocketState.Open);
            socket.ReceiveAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
                .Returns(
                    new WebSocketReceiveResult(4, WebSocketMessageType.Text, true),
                    new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, ""));
            socket.CloseAsync(Arg.Any<WebSocketCloseStatus>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            var act = () => _sut.HandleWebSocketAsync(socket);

            await act.Should().NotThrowAsync();
            await socket.Received(2).ReceiveAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>());
            await socket.Received(1).CloseAsync(WebSocketCloseStatus.NormalClosure, "Encerrado pelo servidor", CancellationToken.None);
        }
    }
}

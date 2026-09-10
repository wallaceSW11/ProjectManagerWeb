using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

[SupportedOSPlatform("windows")]
internal class WindowsRedeColetor : IRedeColetor
{
    private static readonly TimeSpan IntervaloAtualizacaoInterfaces = TimeSpan.FromSeconds(30);

    private readonly Stopwatch _cronometro = new();
    private long _downloadAnterior;
    private long _uploadAnterior;
    private bool _possuiAmostraAnterior;
    private NetworkInterface[] _interfacesAtivas = [];
    private DateTime _proximaAtualizacaoInterfaces = DateTime.MinValue;

    public (long?, long?) ObterBytesPorSegundo()
    {
        var (download, upload) = LerTotais();
        var decorridoSegundos = _cronometro.Elapsed.TotalSeconds;
        _cronometro.Restart();

        if (!_possuiAmostraAnterior)
        {
            _downloadAnterior = download;
            _uploadAnterior = upload;
            _possuiAmostraAnterior = true;
            return (null, null);
        }

        var downloadDelta = Math.Max(0, download - _downloadAnterior);
        var uploadDelta = Math.Max(0, upload - _uploadAnterior);
        _downloadAnterior = download;
        _uploadAnterior = upload;

        if (decorridoSegundos <= 0)
            return (null, null);

        return ((long)(downloadDelta / decorridoSegundos), (long)(uploadDelta / decorridoSegundos));
    }

    private (long download, long upload) LerTotais()
    {
        long download = 0;
        long upload = 0;

        foreach (var rede in ObterInterfacesAtivas())
        {
            try
            {
                var estatisticas = rede.GetIPStatistics();
                download += estatisticas.BytesReceived;
                upload += estatisticas.BytesSent;
            }
            catch
            {
            }
        }

        return (download, upload);
    }

    private NetworkInterface[] ObterInterfacesAtivas()
    {
        if (DateTime.UtcNow < _proximaAtualizacaoInterfaces)
            return _interfacesAtivas;

        _interfacesAtivas = [.. NetworkInterface.GetAllNetworkInterfaces().Where(rede =>
            rede.OperationalStatus == OperationalStatus.Up &&
            rede.NetworkInterfaceType != NetworkInterfaceType.Loopback)];
        _proximaAtualizacaoInterfaces = DateTime.UtcNow + IntervaloAtualizacaoInterfaces;
        return _interfacesAtivas;
    }
}

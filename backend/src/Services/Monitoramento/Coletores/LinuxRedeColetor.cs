using System.Diagnostics;
using System.Globalization;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

internal class LinuxRedeColetor : IRedeColetor
{
    private const string CaminhoNetDev = "/proc/net/dev";

    private readonly string _caminhoNetDev;
    private readonly Stopwatch _cronometro = new();
    private long _downloadAnterior;
    private long _uploadAnterior;
    private bool _possuiAmostraAnterior;

    public LinuxRedeColetor() : this(CaminhoNetDev)
    {
    }

    internal LinuxRedeColetor(string caminhoNetDev)
    {
        _caminhoNetDev = caminhoNetDev;
    }

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

        try
        {
            foreach (var linha in File.ReadLines(_caminhoNetDev).Skip(2))
            {
                try
                {
                    var partes = linha.Split(':', 2, StringSplitOptions.TrimEntries);
                    if (partes.Length != 2 || partes[0] == "lo")
                        continue;

                    var campos = partes[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (campos.Length < 16)
                        continue;

                    download += long.Parse(campos[0], CultureInfo.InvariantCulture);
                    upload += long.Parse(campos[8], CultureInfo.InvariantCulture);
                }
                catch
                {
                }
            }
        }
        catch
        {
            return (0, 0);
        }

        return (download, upload);
    }
}

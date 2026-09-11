using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

[SupportedOSPlatform("windows")]
internal class WindowsDiscoIOColetor : IDiscoIOColetor
{
    private const uint IoctlDiskPerformance = 0x00070020;
    private const uint AcessoLeituraAtributos = 0x80;
    private const uint CompartilhamentoLeitura = 0x1;
    private const uint CompartilhamentoEscrita = 0x2;
    private const uint AbrirExistente = 3;
    private const int MaximoDiscosFisicos = 16;
    private const int TamanhoDiskPerformance = 88;
    private const double UnidadesPorSegundo = 10_000_000.0;
    private const double UnidadesPorMilissegundo = 10_000.0;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    private readonly List<SafeFileHandle> _discos = [];
    private readonly Stopwatch _cronometro = Stopwatch.StartNew();
    private long _bytesLidosAnterior;
    private long _bytesEscritosAnterior;
    private long _tempoLeituraAnterior;
    private long _tempoEscritaAnterior;
    private long _leiturasAnterior;
    private bool _possuiAmostraAnterior;

    public WindowsDiscoIOColetor()
    {
        for (var numero = 0; numero < MaximoDiscosFisicos; numero++)
        {
            var disco = CreateFileW(
                $@"\\.\PhysicalDrive{numero}",
                AcessoLeituraAtributos,
                CompartilhamentoLeitura | CompartilhamentoEscrita,
                IntPtr.Zero,
                AbrirExistente,
                0,
                IntPtr.Zero);

            if (!disco.IsInvalid)
                _discos.Add(disco);
        }
    }

    public (long? leituraBytesPorSegundo, long? escritaBytesPorSegundo, double? atividadePercentual, double? latenciaLeituraMs) ObterMetricas()
    {
        var (bytesLidos, bytesEscritos, tempoLeitura, tempoEscrita, leituras) = LerTotais();
        var decorridoSegundos = _cronometro.Elapsed.TotalSeconds;
        _cronometro.Restart();

        if (!_possuiAmostraAnterior)
        {
            SalvarAmostra(bytesLidos, bytesEscritos, tempoLeitura, tempoEscrita, leituras);
            return (null, null, null, null);
        }

        var deltaBytesLidos = Math.Max(0, bytesLidos - _bytesLidosAnterior);
        var deltaBytesEscritos = Math.Max(0, bytesEscritos - _bytesEscritosAnterior);
        var deltaTempoLeitura = Math.Max(0, tempoLeitura - _tempoLeituraAnterior);
        var deltaTempoEscrita = Math.Max(0, tempoEscrita - _tempoEscritaAnterior);
        var deltaLeituras = Math.Max(0, leituras - _leiturasAnterior);
        SalvarAmostra(bytesLidos, bytesEscritos, tempoLeitura, tempoEscrita, leituras);

        if (decorridoSegundos <= 0)
            return (null, null, null, null);

        var leitura = (long)(deltaBytesLidos / decorridoSegundos);
        var escrita = (long)(deltaBytesEscritos / decorridoSegundos);
        var atividade = Math.Clamp(
            (deltaTempoLeitura + deltaTempoEscrita) / (decorridoSegundos * UnidadesPorSegundo) * 100.0,
            0.0,
            100.0);
        var latencia = deltaLeituras > 0
            ? deltaTempoLeitura / (double)deltaLeituras / UnidadesPorMilissegundo
            : (double?)null;

        return (leitura, escrita, atividade, latencia);
    }

    private (long bytesLidos, long bytesEscritos, long tempoLeitura, long tempoEscrita, long leituras) LerTotais()
    {
        long bytesLidos = 0;
        long bytesEscritos = 0;
        long tempoLeitura = 0;
        long tempoEscrita = 0;
        long leituras = 0;

        var buffer = Marshal.AllocHGlobal(TamanhoDiskPerformance);
        try
        {
            foreach (var disco in _discos)
            {
                if (!DeviceIoControl(disco, IoctlDiskPerformance, IntPtr.Zero, 0, buffer, TamanhoDiskPerformance, out var retornado, IntPtr.Zero))
                    continue;

                if (retornado < TamanhoDiskPerformance)
                    continue;

                bytesLidos += Marshal.ReadInt64(buffer, 0);
                bytesEscritos += Marshal.ReadInt64(buffer, 8);
                tempoLeitura += Marshal.ReadInt64(buffer, 16);
                tempoEscrita += Marshal.ReadInt64(buffer, 24);
                leituras += (uint)Marshal.ReadInt32(buffer, 40);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return (bytesLidos, bytesEscritos, tempoLeitura, tempoEscrita, leituras);
    }

    private void SalvarAmostra(long bytesLidos, long bytesEscritos, long tempoLeitura, long tempoEscrita, long leituras)
    {
        _bytesLidosAnterior = bytesLidos;
        _bytesEscritosAnterior = bytesEscritos;
        _tempoLeituraAnterior = tempoLeitura;
        _tempoEscritaAnterior = tempoEscrita;
        _leiturasAnterior = leituras;
        _possuiAmostraAnterior = true;
    }
}

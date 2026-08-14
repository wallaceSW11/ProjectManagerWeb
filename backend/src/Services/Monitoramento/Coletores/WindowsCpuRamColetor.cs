using System.Runtime.InteropServices;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

internal class WindowsCpuRamColetor : ICpuRamColetor
{
    [DllImport("kernel32.dll")]
    private static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    private long _idleAnterior;
    private long _kernelAnterior;
    private long _userAnterior;
    private bool _possuiAmostraAnterior;

    public string ObterSistemaOperacional() => RuntimeInformation.OSDescription;

    public double? ObterCpuPercentual()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
            return null;

        if (!_possuiAmostraAnterior)
        {
            _idleAnterior = idle;
            _kernelAnterior = kernel;
            _userAnterior = user;
            _possuiAmostraAnterior = true;
            return null;
        }

        var deltaIdle = idle - _idleAnterior;
        var deltaTotal = kernel + user - _kernelAnterior - _userAnterior;
        _idleAnterior = idle;
        _kernelAnterior = kernel;
        _userAnterior = user;

        if (deltaTotal == 0)
            return null;

        return (1.0 - (double)deltaIdle / deltaTotal) * 100.0;
    }

    public (long total, long disponivel) ObterMemoria()
    {
        var status = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status))
            return (0, 0);

        return ((long)status.ullTotalPhys, (long)status.ullAvailPhys);
    }
}

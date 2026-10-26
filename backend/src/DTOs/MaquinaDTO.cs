using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.DTOs;

public sealed record MaquinaDTO(
    string? SistemaOperacional,
    string? CpuNome,
    int Nucleos,
    int Threads,
    long? RamTotalBytes,
    long? DiscoTotalBytes
)
{
    public static MaquinaDTO DoSnapshot(MonitoramentoSnapshotDTO snapshot) =>
        new(
            snapshot.SistemaOperacional,
            snapshot.CpuNome,
            HardwareInfo.NucleosFisicos,
            Environment.ProcessorCount,
            snapshot.RamTotalBytes,
            snapshot.DiscoTotalBytes);
}

namespace ProjectManagerWeb.src.DTOs;

public sealed record MonitoramentoSnapshotDTO(
    DateTime Timestamp,
    string Plataforma,
    int ClientesConectados,
    int ContadorSnapshots,
    string SistemaOperacional,
    double? CpuPercentual,
    long? RamTotalBytes,
    long? RamDisponivelBytes,
    long? RamUsadaBytes
);

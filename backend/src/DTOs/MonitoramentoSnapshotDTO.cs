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
    long? RamUsadaBytes,
    double? DiscoPercentual,
    long? DiscoTotalBytes,
    long? DiscoDisponivelBytes,
    long? DiscoUsadaBytes,
    string? CpuNome,
    double? CpuFrequenciaMhz,
    double? CpuTemperaturaCelsius,
    double? RamVelocidadeMhz,
    double? DiscoTemperaturaCelsius,
    long? SwapTotalBytes,
    long? SwapUsadaBytes,
    long? RedeDownloadBytesPorSegundo,
    long? RedeUploadBytesPorSegundo
);

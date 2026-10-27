namespace ProjectManagerWeb.src.DTOs;

public sealed record UsoAmostraDTO(
    DateTimeOffset Ts,
    double? Cpu,
    double? RamPercentual,
    long? RamUsadaBytes,
    long? DiscoLivreBytes,
    double? DiscoAtividadePercentual,
    double? DiscoLatenciaLeituraMs,
    double? CpuFrequenciaMhz,
    double? CpuTemperaturaCelsius,
    double? DiscoTemperaturaCelsius,
    int ColetaMs,
    double PmwCpu,
    List<UsoProcessoDTO>? Processos,
    MaquinaDTO? Maquina = null,
    long? RamLivreBytes = null,
    string? MetricaCpu = null
);

public sealed record UsoProcessoDTO(
    string Nome,
    double Cpu
);

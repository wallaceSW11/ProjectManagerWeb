namespace ProjectManagerWeb.src.DTOs;

public sealed record ProcessoInfoDTO(
    string Nome,
    double Percentual,
    long? MemoriaBytes
);

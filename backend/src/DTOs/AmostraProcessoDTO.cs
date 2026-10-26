namespace ProjectManagerWeb.src.DTOs;

public readonly record struct AmostraProcessoDTO(
    int Pid,
    string Nome,
    double CpuSegundos
);

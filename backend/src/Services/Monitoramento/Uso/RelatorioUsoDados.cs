namespace ProjectManagerWeb.src.Services.Monitoramento.Uso;

internal sealed class DadosRelatorio
{
    public DateTimeOffset GeradoEm { get; set; }
    public MaquinaRelatorio? Maquina { get; set; }
    public ResumoRelatorio Resumo { get; set; } = new();
    public List<string> TopProcessos { get; set; } = [];
    public List<RegistroRelatorio> Registros { get; set; } = [];
    public int RegistrosOmitidos { get; set; }
}

internal sealed class MaquinaRelatorio
{
    public string? SistemaOperacional { get; set; }
    public string? CpuNome { get; set; }
    public int Nucleos { get; set; }
    public int Threads { get; set; }
    public long? RamTotalBytes { get; set; }
    public long? DiscoTotalBytes { get; set; }
}

internal sealed class ResumoRelatorio
{
    public int Registros { get; set; }
    public double MinutosRegistrados { get; set; }
    public double? LimiarCpu { get; set; }
}

internal sealed class ProcessoRelatorio
{
    public string Nome { get; set; } = "";
    public double Cpu { get; set; }
}

internal sealed class RegistroRelatorio
{
    public DateTimeOffset Ts { get; set; }
    public double? Cpu { get; set; }
    public double? Ram { get; set; }
    public long? RamUsada { get; set; }
    public long? DiscoLivre { get; set; }
    public double? Atividade { get; set; }
    public double? Latencia { get; set; }
    public double? Freq { get; set; }
    public double? TempCpu { get; set; }
    public double? TempDisco { get; set; }
    public int Coleta { get; set; }
    public double Pmw { get; set; }
    public List<ProcessoRelatorio>? Proc { get; set; }
}

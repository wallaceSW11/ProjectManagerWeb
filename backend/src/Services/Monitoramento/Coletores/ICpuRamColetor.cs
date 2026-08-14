namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public interface ICpuRamColetor
{
    string ObterSistemaOperacional();
    string? ObterCpuNome();
    double? ObterCpuFrequenciaMhz();
    double? ObterCpuTemperaturaCelsius();
    double? ObterCpuPercentual();
    (long total, long disponivel) ObterMemoria();
}

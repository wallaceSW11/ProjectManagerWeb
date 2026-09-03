namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public interface ICpuRamColetor
{
    string ObterSistemaOperacional();
    string? ObterCpuNome();
    double? ObterCpuFrequenciaMhz();
    double? ObterCpuTemperaturaCelsius();
    double? ObterDiscoTemperaturaCelsius();
    double? ObterCpuPercentual();
    double? ObterRamVelocidadeMhz();
    double? ObterCoolerRpm();
    (long total, long disponivel) ObterMemoria();
    (long total, long usado) ObterSwap();
}

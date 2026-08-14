namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public interface ICpuRamColetor
{
    string ObterSistemaOperacional();
    double? ObterCpuPercentual();
    (long total, long disponivel) ObterMemoria();
}

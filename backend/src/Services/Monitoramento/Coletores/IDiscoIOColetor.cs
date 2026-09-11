namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public interface IDiscoIOColetor
{
    (long? leituraBytesPorSegundo, long? escritaBytesPorSegundo, double? atividadePercentual, double? latenciaLeituraMs) ObterMetricas();
}

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public interface IRedeColetor
{
    (long? downloadBytesPorSegundo, long? uploadBytesPorSegundo) ObterBytesPorSegundo();
}

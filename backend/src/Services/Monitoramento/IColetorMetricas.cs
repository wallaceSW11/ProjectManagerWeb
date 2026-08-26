using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento;

public interface IColetorMetricas
{
    Task<MonitoramentoSnapshotDTO> ColetarAsync(CancellationToken ct);
}

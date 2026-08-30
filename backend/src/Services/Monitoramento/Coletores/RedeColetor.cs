using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public class RedeColetor(IRedeColetor coletorPlataforma) : IColetorMetricas
{
    public Task<MonitoramentoSnapshotDTO> ColetarAsync(CancellationToken ct)
    {
        var (download, upload) = coletorPlataforma.ObterBytesPorSegundo();

        return Task.FromResult(new MonitoramentoSnapshotDTO(
            DateTime.UtcNow,
            OperatingSystem.IsWindows() ? "windows" : "linux",
            0,
            0,
            "",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            download,
            upload
        ));
    }
}

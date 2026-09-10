using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public class RedeColetor(IRedeColetor coletorPlataforma) : IColetorMetricas
{
    public Task<MonitoramentoSnapshotDTO> ColetarAsync(CancellationToken ct)
    {
        var (download, upload) = coletorPlataforma.ObterBytesPorSegundo();

        return Task.FromResult(new MonitoramentoSnapshotDTO(
            OperatingSystem.IsWindows() ? "windows" : "linux",
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
            download,
            upload
        ));
    }
}

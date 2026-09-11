using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public class DiscoIOColetor(IDiscoIOColetor coletorPlataforma) : IColetorMetricas
{
    public Task<MonitoramentoSnapshotDTO> ColetarAsync(CancellationToken ct)
    {
        var (leitura, escrita, atividade, latencia) = coletorPlataforma.ObterMetricas();

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
            null,
            null,
            leitura,
            escrita,
            atividade,
            latencia
        ));
    }
}

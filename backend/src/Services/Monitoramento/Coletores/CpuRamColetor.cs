using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public class CpuRamColetor(ICpuRamColetor coletorPlataforma) : IColetorMetricas
{
    public Task<MonitoramentoSnapshotDTO> ColetarAsync(CancellationToken ct)
    {
        var cpu = coletorPlataforma.ObterCpuPercentual();
        var (total, disponivel) = coletorPlataforma.ObterMemoria();
        var usado = total - disponivel;
        var (swapTotal, swapUsado) = coletorPlataforma.ObterSwap();

        return Task.FromResult(new MonitoramentoSnapshotDTO(
            DateTime.UtcNow,
            OperatingSystem.IsWindows() ? "windows" : "linux",
            0,
            0,
            coletorPlataforma.ObterSistemaOperacional(),
            cpu,
            total > 0 ? total : null,
            total > 0 ? disponivel : null,
            total > 0 ? usado : null,
            null,
            null,
            null,
            null,
            coletorPlataforma.ObterCpuNome(),
            coletorPlataforma.ObterCpuFrequenciaMhz(),
            coletorPlataforma.ObterCpuTemperaturaCelsius(),
            coletorPlataforma.ObterRamVelocidadeMhz(),
            coletorPlataforma.ObterDiscoTemperaturaCelsius(),
            swapTotal > 0 ? swapTotal : null,
            swapTotal > 0 ? swapUsado : null,
            null,
            null
        ));
    }
}

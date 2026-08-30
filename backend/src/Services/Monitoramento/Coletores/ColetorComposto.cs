using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public class ColetorComposto(IColetorMetricas[] coletores) : IColetorMetricas
{
    public async Task<MonitoramentoSnapshotDTO> ColetarAsync(CancellationToken ct)
    {
        MonitoramentoSnapshotDTO? resultado = null;

        foreach (var coletor in coletores)
        {
            var atual = await coletor.ColetarAsync(ct);
            resultado = resultado is null ? atual : Mesclar(resultado, atual);
        }

        return resultado ?? throw new Exception("Nenhum coletor de métricas configurado");
    }

    private static MonitoramentoSnapshotDTO Mesclar(MonitoramentoSnapshotDTO a, MonitoramentoSnapshotDTO b) =>
        a with
        {
            Timestamp = b.Timestamp,
            Plataforma = string.IsNullOrWhiteSpace(b.Plataforma) ? a.Plataforma : b.Plataforma,
            SistemaOperacional = string.IsNullOrWhiteSpace(b.SistemaOperacional)
                ? a.SistemaOperacional
                : b.SistemaOperacional,
            CpuPercentual = b.CpuPercentual ?? a.CpuPercentual,
            RamTotalBytes = b.RamTotalBytes ?? a.RamTotalBytes,
            RamDisponivelBytes = b.RamDisponivelBytes ?? a.RamDisponivelBytes,
            RamUsadaBytes = b.RamUsadaBytes ?? a.RamUsadaBytes,
            DiscoPercentual = b.DiscoPercentual ?? a.DiscoPercentual,
            DiscoTotalBytes = b.DiscoTotalBytes ?? a.DiscoTotalBytes,
            DiscoDisponivelBytes = b.DiscoDisponivelBytes ?? a.DiscoDisponivelBytes,
            DiscoUsadaBytes = b.DiscoUsadaBytes ?? a.DiscoUsadaBytes,
            CpuNome = b.CpuNome ?? a.CpuNome,
            CpuFrequenciaMhz = b.CpuFrequenciaMhz ?? a.CpuFrequenciaMhz,
            CpuTemperaturaCelsius = b.CpuTemperaturaCelsius ?? a.CpuTemperaturaCelsius,
            RamVelocidadeMhz = b.RamVelocidadeMhz ?? a.RamVelocidadeMhz,
            DiscoTemperaturaCelsius = b.DiscoTemperaturaCelsius ?? a.DiscoTemperaturaCelsius,
            SwapTotalBytes = b.SwapTotalBytes ?? a.SwapTotalBytes,
            SwapUsadaBytes = b.SwapUsadaBytes ?? a.SwapUsadaBytes,
            RedeDownloadBytesPorSegundo = b.RedeDownloadBytesPorSegundo ?? a.RedeDownloadBytesPorSegundo,
            RedeUploadBytesPorSegundo = b.RedeUploadBytesPorSegundo ?? a.RedeUploadBytesPorSegundo
        };
}

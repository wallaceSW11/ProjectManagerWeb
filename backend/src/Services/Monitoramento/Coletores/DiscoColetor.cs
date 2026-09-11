using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public class DiscoColetor : IColetorMetricas
{
    public Task<MonitoramentoSnapshotDTO> ColetarAsync(CancellationToken ct)
    {
        var plataforma = OperatingSystem.IsWindows() ? "windows" : "linux";

        try
        {
            var raiz = Path.GetPathRoot(Directory.GetCurrentDirectory());
            if (string.IsNullOrWhiteSpace(raiz))
                return Task.FromResult(SnapshotVazio(plataforma));

            var drive = new DriveInfo(raiz);
            var total = drive.TotalSize;
            var disponivel = drive.AvailableFreeSpace;
            var usada = total - disponivel;

            if (total <= 0)
                return Task.FromResult(SnapshotVazio(plataforma));

            return Task.FromResult(new MonitoramentoSnapshotDTO(
                plataforma,
                "",
                null,
                null,
                null,
                (double)usada / total * 100.0,
                total,
                disponivel,
                usada,
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
                null
            ));
        }
        catch
        {
            return Task.FromResult(SnapshotVazio(plataforma));
        }
    }

    private static MonitoramentoSnapshotDTO SnapshotVazio(string plataforma) =>
        new(plataforma, "", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
}

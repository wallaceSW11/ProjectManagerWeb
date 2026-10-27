using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public class DiscoColetor : IColetorMetricas
{
    private DriveInfo? _drive;

    public Task<MonitoramentoSnapshotDTO> ColetarAsync(CancellationToken ct)
    {
        var plataforma = OperatingSystem.IsWindows() ? "windows" : "linux";

        try
        {
            var drive = ObterDrive();
            if (drive is null)
                return Task.FromResult(SnapshotVazio(plataforma));

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

    private DriveInfo? ObterDrive()
    {
        if (_drive is not null)
            return _drive;

        var raiz = Path.GetPathRoot(Directory.GetCurrentDirectory());
        if (string.IsNullOrWhiteSpace(raiz))
            return null;

        _drive = new DriveInfo(raiz);
        return _drive;
    }

    private static MonitoramentoSnapshotDTO SnapshotVazio(string plataforma) =>
        new(plataforma, "", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
}

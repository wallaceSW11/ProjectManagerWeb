using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public interface IProcessosColetor
{
    Task<List<ProcessoInfoDTO>> ColetarTopAsync(string tipo, CancellationToken ct);
}

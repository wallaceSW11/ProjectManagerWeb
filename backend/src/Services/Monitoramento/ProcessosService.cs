using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.src.Services.Monitoramento;

public class ProcessosService(IProcessosColetor coletor)
{
    public async Task<List<ProcessoInfoDTO>> ObterTopAsync(string tipo, CancellationToken ct)
    {
        var tipoNormalizado = tipo.Trim().ToLowerInvariant();
        if (tipoNormalizado is not ("cpu" or "ram"))
            throw new ArgumentException("Tipo inválido. Informe 'cpu' ou 'ram'.");

        return await coletor.ColetarTopAsync(tipoNormalizado, ct);
    }
}

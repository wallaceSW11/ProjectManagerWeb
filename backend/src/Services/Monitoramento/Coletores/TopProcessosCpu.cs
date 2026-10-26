using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

public static class TopProcessosCpu
{
    public static List<ProcessoInfoDTO> Calcular(
        Dictionary<int, AmostraProcessoDTO> anterior,
        Dictionary<int, AmostraProcessoDTO> atual,
        double decorridoSegundos,
        int quantidade)
    {
        if (decorridoSegundos <= 0 || anterior.Count == 0)
            return [];

        var nucleos = Environment.ProcessorCount;
        var processos = new List<ProcessoInfoDTO>(atual.Count);

        foreach (var par in atual)
        {
            if (!anterior.TryGetValue(par.Key, out var amostraAnterior))
                continue;

            var deltaSegundos = par.Value.CpuSegundos - amostraAnterior.CpuSegundos;
            var percentual = deltaSegundos / decorridoSegundos / nucleos * 100.0;
            processos.Add(new ProcessoInfoDTO(par.Value.Nome, percentual, null));
        }

        return [.. processos.OrderByDescending(p => p.Percentual).Take(quantidade)];
    }
}

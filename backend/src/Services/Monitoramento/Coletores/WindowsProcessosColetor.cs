using System.Diagnostics;
using System.Runtime.Versioning;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

[SupportedOSPlatform("windows")]
internal class WindowsProcessosColetor(ICpuRamColetor cpuRamColetor) : IProcessosColetor
{
    private const int IntervaloAmostrasMs = 500;
    private const int QuantidadeTop = 10;

    public async Task<List<ProcessoInfoDTO>> ColetarTopAsync(string tipo, CancellationToken ct)
    {
        if (tipo == "ram")
            return ObterTopMemoria();

        var primeira = ObterAmostraCpu();
        await Task.Delay(IntervaloAmostrasMs, ct);
        var segunda = ObterAmostraCpu();

        var nucleos = Environment.ProcessorCount;
        var decorridoSegundos = IntervaloAmostrasMs / 1000.0;
        var processos = new List<ProcessoInfoDTO>();

        foreach (var par in segunda)
        {
            if (!primeira.TryGetValue(par.Key, out var anterior))
                continue;

            var deltaSegundos = (par.Value.CpuTotal - anterior.CpuTotal).TotalSeconds;
            var percentual = deltaSegundos / decorridoSegundos / nucleos * 100.0;
            processos.Add(new ProcessoInfoDTO(par.Value.Nome, percentual, null));
        }

        return processos.OrderByDescending(p => p.Percentual).Take(QuantidadeTop).ToList();
    }

    private List<ProcessoInfoDTO> ObterTopMemoria()
    {
        var total = cpuRamColetor.ObterMemoria().total;
        var processos = new List<ProcessoInfoDTO>();

        foreach (var processo in Process.GetProcesses())
        {
            try
            {
                var memoria = processo.WorkingSet64;
                var percentual = total > 0 ? memoria * 100.0 / total : 0;
                processos.Add(new ProcessoInfoDTO(processo.ProcessName, percentual, memoria));
            }
            catch
            {
            }
            finally
            {
                processo.Dispose();
            }
        }

        return processos.OrderByDescending(p => p.MemoriaBytes).Take(QuantidadeTop).ToList();
    }

    private static Dictionary<int, AmostraProcessoCpu> ObterAmostraCpu()
    {
        var amostra = new Dictionary<int, AmostraProcessoCpu>();

        foreach (var processo in Process.GetProcesses())
        {
            try
            {
                amostra[processo.Id] = new AmostraProcessoCpu(processo.ProcessName, processo.TotalProcessorTime);
            }
            catch
            {
            }
            finally
            {
                processo.Dispose();
            }
        }

        return amostra;
    }

    private readonly record struct AmostraProcessoCpu(string Nome, TimeSpan CpuTotal);
}

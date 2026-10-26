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

        return TopProcessosCpu.Calcular(primeira, segunda, IntervaloAmostrasMs / 1000.0, QuantidadeTop);
    }

    public Dictionary<int, AmostraProcessoDTO> ObterAmostraCpu()
    {
        var amostra = new Dictionary<int, AmostraProcessoDTO>();

        foreach (var processo in Process.GetProcesses())
        {
            try
            {
                amostra[processo.Id] = new AmostraProcessoDTO(
                    processo.Id,
                    processo.ProcessName,
                    processo.TotalProcessorTime.TotalSeconds);
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
}

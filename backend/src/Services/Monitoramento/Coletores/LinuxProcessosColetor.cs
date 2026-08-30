using System.Runtime.InteropServices;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

internal class LinuxProcessosColetor : IProcessosColetor
{
    private const int IntervaloAmostrasMs = 500;
    private const int QuantidadeTop = 10;
    private const string CaminhoProc = "/proc";
    private const int ScClkTck = 2;

    [DllImport("libc", EntryPoint = "sysconf")]
    private static extern long Sysconf(int name);

    private readonly ICpuRamColetor _cpuRamColetor;
    private readonly string _caminhoProc;
    private readonly long _ticksPorSegundo = ObterTicksPorSegundo();

    public LinuxProcessosColetor(ICpuRamColetor cpuRamColetor) : this(cpuRamColetor, CaminhoProc)
    {
    }

    internal LinuxProcessosColetor(ICpuRamColetor cpuRamColetor, string caminhoProc)
    {
        _cpuRamColetor = cpuRamColetor;
        _caminhoProc = caminhoProc;
    }

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

            var deltaSegundos = (par.Value.CpuTicks - anterior.CpuTicks) / (double)_ticksPorSegundo;
            var percentual = deltaSegundos / decorridoSegundos / nucleos * 100.0;
            processos.Add(new ProcessoInfoDTO(par.Value.Nome, percentual, null));
        }

        return processos.OrderByDescending(p => p.Percentual).Take(QuantidadeTop).ToList();
    }

    private List<ProcessoInfoDTO> ObterTopMemoria()
    {
        var total = _cpuRamColetor.ObterMemoria().total;
        var processos = new List<ProcessoInfoDTO>();

        foreach (var pid in EnumerarPids())
        {
            try
            {
                var status = File.ReadAllText(Path.Combine(_caminhoProc, pid.ToString(), "status"));
                var linhaVmRss = status.Split('\n')
                    .FirstOrDefault(l => l.StartsWith("VmRSS:", StringComparison.Ordinal));
                if (linhaVmRss is null)
                    continue;

                var memoriaKb = long.Parse(linhaVmRss.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
                var memoria = memoriaKb * 1024;
                var percentual = total > 0 ? memoria * 100.0 / total : 0;
                processos.Add(new ProcessoInfoDTO(LerNomeProcesso(pid), percentual, memoria));
            }
            catch
            {
            }
        }

        return processos.OrderByDescending(p => p.MemoriaBytes).Take(QuantidadeTop).ToList();
    }

    private Dictionary<int, AmostraProcessoCpu> ObterAmostraCpu()
    {
        var amostra = new Dictionary<int, AmostraProcessoCpu>();

        foreach (var pid in EnumerarPids())
        {
            try
            {
                var stat = File.ReadAllText(Path.Combine(_caminhoProc, pid.ToString(), "stat"));
                var fecho = stat.LastIndexOf(')');
                if (fecho < 0)
                    continue;

                var campos = stat[(fecho + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (campos.Length < 13)
                    continue;

                var utime = long.Parse(campos[11]);
                var stime = long.Parse(campos[12]);
                amostra[pid] = new AmostraProcessoCpu(LerNomeProcesso(pid), utime + stime);
            }
            catch
            {
            }
        }

        return amostra;
    }

    private IEnumerable<int> EnumerarPids()
    {
        foreach (var diretorio in Directory.EnumerateDirectories(_caminhoProc))
        {
            var nome = Path.GetFileName(diretorio);
            if (int.TryParse(nome, out var pid))
                yield return pid;
        }
    }

    private string LerNomeProcesso(int pid)
    {
        var nome = LerNomeExecutavel(pid);
        if (!string.IsNullOrWhiteSpace(nome))
            return nome;

        nome = LerPrimeiroArgumento(pid);
        if (!string.IsNullOrWhiteSpace(nome))
            return nome;

        return LerTexto(Path.Combine(_caminhoProc, pid.ToString(), "comm"));
    }

    private string LerNomeExecutavel(int pid)
    {
        try
        {
            var alvo = new FileInfo(Path.Combine(_caminhoProc, pid.ToString(), "exe")).LinkTarget;
            if (string.IsNullOrWhiteSpace(alvo))
                return "";

            const string sufixoDeletado = " (deleted)";
            var caminho = alvo.EndsWith(sufixoDeletado, StringComparison.Ordinal)
                ? alvo[..^sufixoDeletado.Length]
                : alvo;

            var indiceBarra = caminho.LastIndexOf('/');
            return indiceBarra >= 0 ? caminho[(indiceBarra + 1)..] : caminho;
        }
        catch
        {
            return "";
        }
    }

    private string LerPrimeiroArgumento(int pid)
    {
        try
        {
            var cmdline = File.ReadAllText(Path.Combine(_caminhoProc, pid.ToString(), "cmdline"));
            if (string.IsNullOrWhiteSpace(cmdline))
                return "";

            var primeiroArgumento = cmdline.Split('\0')[0];
            var indiceBarra = primeiroArgumento.LastIndexOf('/');
            var nome = indiceBarra >= 0 ? primeiroArgumento[(indiceBarra + 1)..] : primeiroArgumento;
            var indiceEspaco = nome.IndexOf(' ');
            return indiceEspaco >= 0 ? nome[..indiceEspaco] : nome;
        }
        catch
        {
            return "";
        }
    }

    private static string LerTexto(string caminho)
    {
        try
        {
            return File.Exists(caminho) ? File.ReadAllText(caminho).Trim() : "";
        }
        catch
        {
            return "";
        }
    }

    private static long ObterTicksPorSegundo()
    {
        try
        {
            var ticks = Sysconf(ScClkTck);
            if (ticks > 0)
                return ticks;
        }
        catch
        {
        }

        return 100;
    }

    private readonly record struct AmostraProcessoCpu(string Nome, long CpuTicks);
}

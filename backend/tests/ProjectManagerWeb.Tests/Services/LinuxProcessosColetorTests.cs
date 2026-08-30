using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class LinuxProcessosColetorTests : IDisposable
{
    private readonly ICpuRamColetor _cpuRamColetor = Substitute.For<ICpuRamColetor>();
    private readonly string _caminhoProc;
    private readonly string _tempPath;

    public LinuxProcessosColetorTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempPath);
        _caminhoProc = Path.Combine(_tempPath, "proc");
        Directory.CreateDirectory(_caminhoProc);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private LinuxProcessosColetor CriarColetor() =>
        new(_cpuRamColetor, _caminhoProc);

    private void EscreverArquivoProcesso(int pid, string nomeArquivo, string conteudo)
    {
        var diretorio = Path.Combine(_caminhoProc, pid.ToString());
        Directory.CreateDirectory(diretorio);
        File.WriteAllText(Path.Combine(diretorio, nomeArquivo), conteudo);
    }

    private static string MontarStat(long utime, long stime) =>
        $"100 (nome) S 1 1 1 0 -1 4194560 10 20 30 40 {utime} {stime}";

    private static string MontarStatus(long vmRssKb) =>
        $"Name:\tprocesso\nVmRSS:\t {vmRssKb} kB\n";

    public class ColetarTopAsyncCpu : LinuxProcessosColetorTests
    {
        [Fact]
        public async Task Deve_calcular_percentual_positivo_quando_processo_consome_cpu()
        {
            EscreverArquivoProcesso(100, "stat", MontarStat(50, 60));
            var coletor = CriarColetor();

            var task = coletor.ColetarTopAsync("cpu", CancellationToken.None);
            await Task.Delay(250);
            EscreverArquivoProcesso(100, "stat", MontarStat(150, 160));
            var processos = await task;

            var esperado = 200.0 / 100 / 0.5 / Environment.ProcessorCount * 100.0;
            processos.Should().ContainSingle();
            processos[0].Percentual.Should().BeApproximately(esperado, 0.5);
        }

        [Fact]
        public async Task Deve_calcular_percentual_zero_quando_processo_nao_muda_entre_amostras()
        {
            EscreverArquivoProcesso(100, "stat", MontarStat(50, 60));
            var coletor = CriarColetor();

            var task = coletor.ColetarTopAsync("cpu", CancellationToken.None);
            await Task.Delay(250);
            EscreverArquivoProcesso(100, "stat", MontarStat(50, 60));
            var processos = await task;

            processos.Should().ContainSingle();
            processos[0].Percentual.Should().Be(0);
        }

        [Fact]
        public async Task Deve_ordenar_processos_por_percentual_descendente()
        {
            EscreverArquivoProcesso(100, "cmdline", "/usr/bin/rapido\0");
            EscreverArquivoProcesso(100, "stat", MontarStat(50, 60));
            EscreverArquivoProcesso(200, "cmdline", "/usr/bin/lento\0");
            EscreverArquivoProcesso(200, "stat", MontarStat(50, 60));
            var coletor = CriarColetor();

            var task = coletor.ColetarTopAsync("cpu", CancellationToken.None);
            await Task.Delay(250);
            EscreverArquivoProcesso(100, "stat", MontarStat(250, 260));
            EscreverArquivoProcesso(200, "stat", MontarStat(150, 160));
            var processos = await task;

            processos.Should().HaveCount(2);
            processos[0].Nome.Should().Be("rapido");
            processos[1].Nome.Should().Be("lento");
            processos[0].Percentual.Should().BeGreaterThan(processos[1].Percentual);
        }
    }

    public class ColetarTopAsyncRam : LinuxProcessosColetorTests
    {
        [Fact]
        public async Task Deve_ordenar_processos_por_memoria_descendente_e_calcular_percentual()
        {
            _cpuRamColetor.ObterMemoria().Returns((total: 4194304L, disponivel: 2097152L));
            EscreverArquivoProcesso(100, "cmdline", "/usr/bin/maior\0");
            EscreverArquivoProcesso(100, "status", MontarStatus(2048));
            EscreverArquivoProcesso(200, "cmdline", "/usr/bin/menor\0");
            EscreverArquivoProcesso(200, "status", MontarStatus(1024));
            var coletor = CriarColetor();

            var processos = await coletor.ColetarTopAsync("ram", CancellationToken.None);

            processos.Should().HaveCount(2);
            processos[0].Nome.Should().Be("maior");
            processos[0].MemoriaBytes.Should().Be(2048L * 1024);
            processos[0].Percentual.Should().BeApproximately(50.0, 0.001);
            processos[1].Nome.Should().Be("menor");
            processos[1].MemoriaBytes.Should().Be(1024L * 1024);
            processos[1].Percentual.Should().BeApproximately(25.0, 0.001);
        }

        [Fact]
        public async Task Deve_limitar_para_dez_processos_quando_ha_mais()
        {
            _cpuRamColetor.ObterMemoria().Returns((total: 4194304L, disponivel: 2097152L));
            for (var pid = 1; pid <= 12; pid++)
                EscreverArquivoProcesso(pid, "status", MontarStatus(1024));
            var coletor = CriarColetor();

            var processos = await coletor.ColetarTopAsync("ram", CancellationToken.None);

            processos.Should().HaveCount(10);
        }

        [Fact]
        public async Task Deve_ignorar_processo_sem_vmrss_no_status()
        {
            _cpuRamColetor.ObterMemoria().Returns((total: 4194304L, disponivel: 2097152L));
            EscreverArquivoProcesso(100, "cmdline", "/usr/bin/ignorado\0");
            EscreverArquivoProcesso(100, "status", "Name:\tignorado\nVmPeak:\t 1024 kB\n");
            EscreverArquivoProcesso(200, "cmdline", "/usr/bin/com-vmrss\0");
            EscreverArquivoProcesso(200, "status", MontarStatus(1024));
            var coletor = CriarColetor();

            var processos = await coletor.ColetarTopAsync("ram", CancellationToken.None);

            processos.Should().ContainSingle();
            processos[0].Nome.Should().Be("com-vmrss");
        }
    }

    public class LerNomeProcesso : LinuxProcessosColetorTests
    {
        [Fact]
        public async Task Deve_usar_basename_do_primeiro_argumento_do_cmdline()
        {
            _cpuRamColetor.ObterMemoria().Returns((total: 4194304L, disponivel: 2097152L));
            EscreverArquivoProcesso(100, "cmdline", "/usr/bin/meuapp\0--flag");
            EscreverArquivoProcesso(100, "status", MontarStatus(1024));
            var coletor = CriarColetor();

            var processos = await coletor.ColetarTopAsync("ram", CancellationToken.None);

            processos.Should().ContainSingle();
            processos[0].Nome.Should().Be("meuapp");
        }

        [Fact]
        public async Task Deve_usar_primeiro_token_quando_primeiro_argumento_tem_espacos()
        {
            _cpuRamColetor.ObterMemoria().Returns((total: 4194304L, disponivel: 2097152L));
            EscreverArquivoProcesso(100, "cmdline", "/opt/chrome/chrome --type=renderer");
            EscreverArquivoProcesso(100, "status", MontarStatus(1024));
            var coletor = CriarColetor();

            var processos = await coletor.ColetarTopAsync("ram", CancellationToken.None);

            processos.Should().ContainSingle();
            processos[0].Nome.Should().Be("chrome");
        }

        [Fact]
        public async Task Deve_usar_comm_quando_cmdline_eh_vazio()
        {
            _cpuRamColetor.ObterMemoria().Returns((total: 4194304L, disponivel: 2097152L));
            EscreverArquivoProcesso(100, "cmdline", "");
            EscreverArquivoProcesso(100, "comm", "kworker/0:1");
            EscreverArquivoProcesso(100, "status", MontarStatus(1024));
            var coletor = CriarColetor();

            var processos = await coletor.ColetarTopAsync("ram", CancellationToken.None);

            processos.Should().ContainSingle();
            processos[0].Nome.Should().Be("kworker/0:1");
        }

        [Fact]
        public async Task Deve_usar_basename_do_symlink_exe_quando_existe()
        {
            _cpuRamColetor.ObterMemoria().Returns((total: 4194304L, disponivel: 2097152L));
            EscreverArquivoProcesso(100, "status", MontarStatus(1024));
            var alvo = Path.Combine(_tempPath, "meuapp-bin");
            File.WriteAllText(alvo, "");
            File.CreateSymbolicLink(Path.Combine(_caminhoProc, "100", "exe"), alvo);
            var coletor = CriarColetor();

            var processos = await coletor.ColetarTopAsync("ram", CancellationToken.None);

            processos.Should().ContainSingle();
            processos[0].Nome.Should().Be("meuapp-bin");
        }
    }
}

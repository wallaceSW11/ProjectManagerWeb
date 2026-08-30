using System.Runtime.InteropServices;
using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class LinuxCpuRamColetorTests : IDisposable
{
    private readonly string _tempPath;

    public LinuxCpuRamColetorTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), "pmw-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, true);
    }

    private LinuxCpuRamColetor CriarColetor() =>
        new(
            Path.Combine(_tempPath, "stat"),
            Path.Combine(_tempPath, "meminfo"),
            Path.Combine(_tempPath, "os-release"),
            Path.Combine(_tempPath, "cpuinfo"),
            Path.Combine(_tempPath, "scaling_cur_freq"),
            Path.Combine(_tempPath, "thermal"),
            Path.Combine(_tempPath, "hwmon"));

    private void EscreverArquivo(string caminhoRelativo, string conteudo)
    {
        var caminho = Path.Combine(_tempPath, caminhoRelativo);
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        File.WriteAllText(caminho, conteudo);
    }

    public class ObterSistemaOperacional : LinuxCpuRamColetorTests
    {
        [Fact]
        public void Deve_retornar_pretty_name_quando_arquivo_os_release_existe()
        {
            EscreverArquivo("os-release", "NAME=\"Ubuntu\"\nPRETTY_NAME=\"Ubuntu 24.04.1 LTS\"\n");
            var coletor = CriarColetor();

            var resultado = coletor.ObterSistemaOperacional();

            resultado.Should().Be("Ubuntu 24.04.1 LTS");
        }

        [Fact]
        public void Deve_retornar_descricao_do_runtime_quando_arquivo_os_release_nao_existe()
        {
            var coletor = CriarColetor();

            var resultado = coletor.ObterSistemaOperacional();

            resultado.Should().Be(RuntimeInformation.OSDescription);
        }
    }

    public class ObterCpuNome : LinuxCpuRamColetorTests
    {
        [Fact]
        public void Deve_retornar_model_name_quando_cpuinfo_existe()
        {
            EscreverArquivo("cpuinfo", "processor\t: 0\nmodel name\t: Intel(R) Core(TM) i7-9700K CPU @ 3.60GHz\n");
            var coletor = CriarColetor();

            var resultado = coletor.ObterCpuNome();

            resultado.Should().Be("Intel(R) Core(TM) i7-9700K CPU @ 3.60GHz");
        }

        [Fact]
        public void Deve_retornar_hardware_quando_cpuinfo_nao_tem_model_name()
        {
            EscreverArquivo("cpuinfo", "Hardware\t: BCM2835\n");
            var coletor = CriarColetor();

            var resultado = coletor.ObterCpuNome();

            resultado.Should().Be("BCM2835");
        }

        [Fact]
        public void Deve_retornar_null_quando_cpuinfo_nao_existe()
        {
            var coletor = CriarColetor();

            var resultado = coletor.ObterCpuNome();

            resultado.Should().BeNull();
        }
    }

    public class ObterCpuFrequenciaMhz : LinuxCpuRamColetorTests
    {
        [Fact]
        public void Deve_retornar_media_das_frequencias_quando_cpuinfo_tem_cpu_mhz()
        {
            EscreverArquivo("cpuinfo", "cpu MHz\t\t: 3600.000\ncpu MHz\t\t: 3400.000\n");
            var coletor = CriarColetor();

            var resultado = coletor.ObterCpuFrequenciaMhz();

            resultado.Should().Be(3500.0);
        }

        [Fact]
        public void Deve_usar_scaling_cur_freq_quando_cpuinfo_nao_tem_cpu_mhz()
        {
            EscreverArquivo("cpuinfo", "processor\t: 0\n");
            EscreverArquivo("scaling_cur_freq", "2400000\n");
            var coletor = CriarColetor();

            var resultado = coletor.ObterCpuFrequenciaMhz();

            resultado.Should().Be(2400.0);
        }

        [Fact]
        public void Deve_retornar_null_quando_nenhuma_fonte_de_frequencia_existe()
        {
            var coletor = CriarColetor();

            var resultado = coletor.ObterCpuFrequenciaMhz();

            resultado.Should().BeNull();
        }
    }

    public class ObterCpuPercentual : LinuxCpuRamColetorTests
    {
        [Fact]
        public void Deve_retornar_null_na_primeira_leitura_e_percentual_na_segunda()
        {
            EscreverArquivo("stat", "cpu  100 0 100 1000 0 0 0 0\n");
            var coletor = CriarColetor();

            coletor.ObterCpuPercentual().Should().BeNull();

            EscreverArquivo("stat", "cpu  200 0 200 1100 0 0 0 0\n");
            coletor.ObterCpuPercentual().Should().BeApproximately(66.67, 0.01);
        }

        [Fact]
        public void Deve_retornar_null_quando_arquivo_stat_nao_existe()
        {
            var coletor = CriarColetor();

            coletor.ObterCpuPercentual().Should().BeNull();
        }
    }

    public class ObterMemoria : LinuxCpuRamColetorTests
    {
        [Fact]
        public void Deve_converter_kilobytes_para_bytes_quando_meminfo_existe()
        {
            EscreverArquivo("meminfo", "MemTotal:       16777216 kB\nMemFree:         4194304 kB\nMemAvailable:    8388608 kB\n");
            var coletor = CriarColetor();

            var (total, disponivel) = coletor.ObterMemoria();

            total.Should().Be(16777216L * 1024);
            disponivel.Should().Be(8388608L * 1024);
        }

        [Fact]
        public void Deve_retornar_zero_quando_meminfo_nao_existe()
        {
            var coletor = CriarColetor();

            var (total, disponivel) = coletor.ObterMemoria();

            total.Should().Be(0);
            disponivel.Should().Be(0);
        }
    }

    public class ObterSwap : LinuxCpuRamColetorTests
    {
        [Fact]
        public void Deve_converter_kilobytes_para_bytes_quando_meminfo_tem_swap()
        {
            EscreverArquivo("meminfo", "SwapTotal:       8388608 kB\nSwapFree:        4194304 kB\n");
            var coletor = CriarColetor();

            var (total, usado) = coletor.ObterSwap();

            total.Should().Be(8589934592);
            usado.Should().Be(4294967296);
        }

        [Fact]
        public void Deve_retornar_zero_quando_meminfo_nao_tem_swap_total()
        {
            EscreverArquivo("meminfo", "MemTotal:       16777216 kB\n");
            var coletor = CriarColetor();

            var (total, usado) = coletor.ObterSwap();

            total.Should().Be(0);
            usado.Should().Be(0);
        }
    }

    public class ObterDiscoTemperaturaCelsius : LinuxCpuRamColetorTests
    {
        [Fact]
        public void Deve_converter_miligraus_para_celsius_quando_hwmon_eh_nvme()
        {
            EscreverArquivo("hwmon/hwmon0/name", "nvme\n");
            EscreverArquivo("hwmon/hwmon0/temp1_input", "40850\n");
            var coletor = CriarColetor();

            var resultado = coletor.ObterDiscoTemperaturaCelsius();

            resultado.Should().Be(40.85);
        }

        [Fact]
        public void Deve_retornar_null_quando_hwmon_nao_eh_nvme()
        {
            EscreverArquivo("hwmon/hwmon0/name", "k10temp\n");
            EscreverArquivo("hwmon/hwmon0/temp1_input", "40850\n");
            var coletor = CriarColetor();

            var resultado = coletor.ObterDiscoTemperaturaCelsius();

            resultado.Should().BeNull();
        }
    }

    public class ObterCpuTemperaturaCelsius : LinuxCpuRamColetorTests
    {
        [Fact]
        public void Deve_retornar_temperatura_da_zona_termica_quando_tipo_eh_cpu()
        {
            EscreverArquivo("thermal/thermal_zone0/temp", "52000\n");
            EscreverArquivo("thermal/thermal_zone0/type", "x86_pkg_temp\n");
            var coletor = CriarColetor();

            var resultado = coletor.ObterCpuTemperaturaCelsius();

            resultado.Should().Be(52.0);
        }

        [Fact]
        public void Deve_retornar_temperatura_do_hwmon_quando_nome_eh_coretemp()
        {
            EscreverArquivo("hwmon/hwmon0/temp1_input", "45000\n");
            EscreverArquivo("hwmon/hwmon0/name", "coretemp\n");
            var coletor = CriarColetor();

            var resultado = coletor.ObterCpuTemperaturaCelsius();

            resultado.Should().Be(45.0);
        }

        [Fact]
        public void Deve_retornar_temperatura_fallback_quando_nenhuma_zona_eh_cpu()
        {
            EscreverArquivo("thermal/thermal_zone0/temp", "30000\n");
            EscreverArquivo("thermal/thermal_zone0/type", "acpitz\n");
            var coletor = CriarColetor();

            var resultado = coletor.ObterCpuTemperaturaCelsius();

            resultado.Should().Be(30.0);
        }

        [Fact]
        public void Deve_retornar_null_quando_nao_existem_zonas_termicas_nem_hwmon()
        {
            var coletor = CriarColetor();

            var resultado = coletor.ObterCpuTemperaturaCelsius();

            resultado.Should().BeNull();
        }
    }
}

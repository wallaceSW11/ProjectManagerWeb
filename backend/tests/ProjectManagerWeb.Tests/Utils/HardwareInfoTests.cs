using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.Tests.Utils;

public class HardwareInfoTests
{
    private static string[] CpuInfo(int sockets, int nucleosPorSocket, int threadsPorNucleo)
    {
        var linhas = new List<string>();

        for (var socket = 0; socket < sockets; socket++)
            for (var nucleo = 0; nucleo < nucleosPorSocket; nucleo++)
                for (var thread = 0; thread < threadsPorNucleo; thread++)
                    linhas.AddRange(
                    [
                        "processor\t: " + (socket * nucleosPorSocket * threadsPorNucleo + nucleo * threadsPorNucleo + thread),
                        "physical id\t: " + socket,
                        "core id\t\t: " + nucleo,
                        ""
                    ]);

        return [.. linhas];
    }

    public class ContarNucleos : HardwareInfoTests
    {
        [Fact]
        public void Deve_contar_nucleos_fisicos_e_nao_threads()
        {
            var nucleos = HardwareInfo.ContarNucleos(CpuInfo(sockets: 1, nucleosPorSocket: 4, threadsPorNucleo: 2));

            nucleos.Should().Be(4);
        }

        [Fact]
        public void Deve_somar_nucleos_de_todos_os_sockets()
        {
            var nucleos = HardwareInfo.ContarNucleos(CpuInfo(sockets: 2, nucleosPorSocket: 6, threadsPorNucleo: 2));

            nucleos.Should().Be(12);
        }

        [Fact]
        public void Deve_contar_por_core_id_quando_physical_id_nao_existe()
        {
            string[] linhas =
            [
                "processor\t: 0",
                "core id\t\t: 0",
                "processor\t: 1",
                "core id\t\t: 1",
                "processor\t: 2",
                "core id\t\t: 0"
            ];

            HardwareInfo.ContarNucleos(linhas).Should().Be(2);
        }

        [Fact]
        public void Deve_retornar_zero_quando_cpuinfo_nao_tem_core_id()
        {
            string[] linhas =
            [
                "processor\t: 0",
                "model name\t: ARMv8 Processor",
                "processor\t: 1",
                "model name\t: ARMv8 Processor"
            ];

            HardwareInfo.ContarNucleos(linhas).Should().Be(0);
        }

        [Fact]
        public void Deve_ignorar_linhas_sem_dois_pontos()
        {
            string[] linhas =
            [
                "lixo sem separador",
                "core id\t\t: 3",
                "    ",
                "core id\t\t: 3"
            ];

            HardwareInfo.ContarNucleos(linhas).Should().Be(1);
        }
    }
}

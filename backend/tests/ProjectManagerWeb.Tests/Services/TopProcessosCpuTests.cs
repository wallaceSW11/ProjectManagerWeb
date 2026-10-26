using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class TopProcessosCpuTests
{
    private static Dictionary<int, AmostraProcessoDTO> CriarAmostras(params (int Pid, string Nome, double CpuSegundos)[] itens) =>
        itens.ToDictionary(item => item.Pid, item => new AmostraProcessoDTO(item.Pid, item.Nome, item.CpuSegundos));

    public class Calcular : TopProcessosCpuTests
    {
        [Fact]
        public void Deve_calcular_percentual_de_um_nucleo_quando_processo_usa_um_nucleo_inteiro()
        {
            var anterior = CriarAmostras((1, "app", 0));
            var atual = CriarAmostras((1, "app", Environment.ProcessorCount * 4.0));

            var processos = TopProcessosCpu.Calcular(anterior, atual, 4, 10);

            processos.Should().ContainSingle();
            processos[0].Percentual.Should().BeApproximately(100.0, 0.001);
        }

        [Fact]
        public void Deve_ignorar_processo_quando_nao_existe_na_amostra_anterior()
        {
            var anterior = CriarAmostras((1, "app", 0));
            var atual = CriarAmostras((1, "app", 1), (2, "novo", 100));

            var processos = TopProcessosCpu.Calcular(anterior, atual, 1, 10);

            processos.Should().ContainSingle();
            processos[0].Nome.Should().Be("app");
        }

        [Fact]
        public void Deve_ordenar_por_percentual_decrescente()
        {
            var anterior = CriarAmostras((1, "lento", 0), (2, "rapido", 0));
            var atual = CriarAmostras((1, "lento", 1), (2, "rapido", 4));

            var processos = TopProcessosCpu.Calcular(anterior, atual, 4, 10);

            processos[0].Nome.Should().Be("rapido");
            processos[1].Nome.Should().Be("lento");
        }

        [Fact]
        public void Deve_respeitar_quantidade_maxima_de_processos()
        {
            var anterior = CriarAmostras((1, "a", 0), (2, "b", 0), (3, "c", 0));
            var atual = CriarAmostras((1, "a", 4), (2, "b", 2), (3, "c", 1));

            var processos = TopProcessosCpu.Calcular(anterior, atual, 4, 2);

            processos.Should().HaveCount(2);
            processos[0].Nome.Should().Be("a");
        }

        [Fact]
        public void Deve_retornar_vazio_quando_nao_ha_amostra_anterior()
        {
            var atual = CriarAmostras((1, "app", 10));

            var processos = TopProcessosCpu.Calcular(new Dictionary<int, AmostraProcessoDTO>(), atual, 4, 10);

            processos.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Deve_retornar_vazio_quando_intervalo_invalido(double decorridoSegundos)
        {
            var anterior = CriarAmostras((1, "app", 0));
            var atual = CriarAmostras((1, "app", 10));

            var processos = TopProcessosCpu.Calcular(anterior, atual, decorridoSegundos, 10);

            processos.Should().BeEmpty();
        }
    }
}

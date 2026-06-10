using ProjectManagerWeb.src.Services;

namespace ProjectManagerWeb.Tests.Services;

public class VersaoServiceTests
{
    public class CompararVersao
    {
        [Fact]
        public void Deve_retornar_negativo_quando_a_menor_que_b()
        {
            var resultado = VersaoService.CompararVersao("1.0.0", "2.0.0");

            resultado.Should().BeNegative();
        }

        [Fact]
        public void Deve_retornar_zero_quando_a_igual_b()
        {
            var resultado = VersaoService.CompararVersao("1.2.3", "1.2.3");

            resultado.Should().Be(0);
        }

        [Fact]
        public void Deve_retornar_positivo_quando_a_maior_que_b()
        {
            var resultado = VersaoService.CompararVersao("3.0.0", "1.0.0");

            resultado.Should().BePositive();
        }

        [Fact]
        public void Deve_retornar_negativo_quando_a_tem_menos_partes()
        {
            var resultado = VersaoService.CompararVersao("1.0", "1.0.1");

            resultado.Should().BeNegative();
        }

        [Fact]
        public void Deve_retornar_positivo_quando_a_tem_mais_partes()
        {
            var resultado = VersaoService.CompararVersao("1.0.1", "1.0");

            resultado.Should().BePositive();
        }
    }
}

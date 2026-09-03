using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class ValidadorCoolerDinamicoTests
{
    public class Avaliar : ValidadorCoolerDinamicoTests
    {
        [Fact]
        public void Deve_retornar_null_quando_rpm_eh_null()
        {
            var validador = new ValidadorCoolerDinamico();

            var resultado = validador.Avaliar(null);

            resultado.Should().BeNull();
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        public void Deve_retornar_null_quando_rpm_eh_zero_ou_negativo(double? rpm)
        {
            var validador = new ValidadorCoolerDinamico();

            var resultado = validador.Avaliar(rpm);

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_na_primeira_leitura_valida()
        {
            var validador = new ValidadorCoolerDinamico();

            var resultado = validador.Avaliar(2500.0);

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_enquanto_leitura_permanece_dentro_da_tolerancia()
        {
            var validador = new ValidadorCoolerDinamico();

            validador.Avaliar(2500.0).Should().BeNull();
            validador.Avaliar(2500.5).Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_valor_e_tornar_dinamico_quando_rpm_varia_acima_da_tolerancia()
        {
            var validador = new ValidadorCoolerDinamico();

            validador.Avaliar(2500.0).Should().BeNull();
            validador.Avaliar(2800.0).Should().Be(2800.0);
        }

        [Fact]
        public void Deve_retornar_sempre_o_valor_apos_tornar_dinamico()
        {
            var validador = new ValidadorCoolerDinamico();

            validador.Avaliar(2500.0).Should().BeNull();
            validador.Avaliar(2800.0).Should().Be(2800.0);
            validador.Avaliar(1500.0).Should().Be(1500.0);
        }

        [Fact]
        public void Deve_retornar_null_quando_rpm_null_apos_baseline()
        {
            var validador = new ValidadorCoolerDinamico();

            validador.Avaliar(2500.0).Should().BeNull();
            validador.Avaliar(null).Should().BeNull();
            validador.Avaliar(2800.0).Should().Be(2800.0);
        }
    }
}

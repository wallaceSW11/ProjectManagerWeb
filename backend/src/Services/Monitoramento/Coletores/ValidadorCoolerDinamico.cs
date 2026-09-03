namespace ProjectManagerWeb.src.Services.Monitoramento.Coletores;

internal class ValidadorCoolerDinamico
{
    private const double ToleranciaRpm = 1.0;

    private double? _primeiroValor;
    private bool _dinamico;

    public double? Avaliar(double? rpm)
    {
        if (rpm is null or <= 0)
            return null;

        if (_dinamico)
            return rpm;

        if (_primeiroValor is null)
        {
            _primeiroValor = rpm;
            return null;
        }

        if (Math.Abs(rpm.Value - _primeiroValor.Value) > ToleranciaRpm)
        {
            _primeiroValor = null;
            _dinamico = true;
            return rpm;
        }

        return null;
    }
}

namespace ProjectManagerWeb.src.Utils.Terminais;

public interface ITerminalEmulator
{
    void Executar(string command, string? perfilTerminal = null, string? workingDirectory = null, string? complementoTerminal = null);
    List<string> ObterPerfis();
}

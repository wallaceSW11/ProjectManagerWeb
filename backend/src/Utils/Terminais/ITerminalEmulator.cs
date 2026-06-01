namespace ProjectManagerWeb.src.Utils.Terminais;

public interface ITerminalEmulator
{
    void Executar(string command, string? perfilTerminal = null);
    List<string> ObterPerfis();
}

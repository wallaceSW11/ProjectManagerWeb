namespace ProjectManagerWeb.src.Utils;

public interface IShellProvider
{
    void ExecutarComInterface(string command, string? perfilTerminal = null);
    void ExecutarSemInterface(string command);
    void ExecutarComoAdministrador(string command, string? perfilTerminal = null);
    List<string> ObterPerfisTerminal();
}

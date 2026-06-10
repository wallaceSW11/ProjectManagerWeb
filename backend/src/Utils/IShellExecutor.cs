namespace ProjectManagerWeb.src.Utils;

public interface IShellExecutor
{
    void ExecutarComando(string command, string? perfilTerminal = null, string? githubToken = null);
    void ExecutarComandoSemInterface(string command);
}

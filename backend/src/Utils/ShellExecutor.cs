namespace ProjectManagerWeb.src.Utils;

public class ShellExecutor : IShellExecutor
{
    public void ExecutarComando(string command, string? perfilTerminal = null, string? githubToken = null)
    {
        ShellExecute.ExecutarComando(command, perfilTerminal, githubToken);
    }

    public void ExecutarComandoSemInterface(string command)
    {
        ShellExecute.ExecutarComandoSemInterface(command);
    }
}

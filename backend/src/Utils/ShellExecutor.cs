namespace ProjectManagerWeb.src.Utils;

public class ShellExecutor : IShellExecutor
{
    public void ExecutarComando(string command, string? perfilTerminal = null, string? githubToken = null, string? complementoTerminal = null, bool semInterface = false)
    {
        ShellExecute.ExecutarComando(command, perfilTerminal, githubToken, complementoTerminal, semInterface);
    }

    public void ExecutarComandoSemInterface(string command)
    {
        ShellExecute.ExecutarComandoSemInterface(command);
    }

    public void AbrirDiretorio(string caminho)
    {
        ShellExecute.AbrirDiretorio(caminho);
    }

    public void AbrirArquivo(string caminho)
    {
        ShellExecute.AbrirArquivo(caminho);
    }
}

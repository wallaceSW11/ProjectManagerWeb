namespace ProjectManagerWeb.src.Utils;

public interface IShellExecutor
{
    void ExecutarComando(string command, string? perfilTerminal = null, string? githubToken = null, bool semInterface = false);
    void ExecutarComandoSemInterface(string command);
    void AbrirDiretorio(string caminho);
    void AbrirArquivo(string caminho);
}

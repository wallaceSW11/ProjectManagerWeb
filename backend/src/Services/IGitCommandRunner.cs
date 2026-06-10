namespace ProjectManagerWeb.src.Services;

public interface IGitCommandRunner
{
    Task<ComandoResultado> RunAsync(string command, string? sshKey = null);
}

public sealed record ComandoResultado(string Output, string Error, int ExitCode);

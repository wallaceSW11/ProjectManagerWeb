namespace ProjectManagerWeb.src.Utils.Terminais;

public static class TerminalEmulatorFactory
{
    public static ITerminalEmulator Criar(string? terminal) => terminal?.ToLower() switch
    {
        "ghostty" => new GhosttyTerminal(),
        _ => new PtyxisTerminal()
    };
}

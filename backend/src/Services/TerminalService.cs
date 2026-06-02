using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services;

public class TerminalService(IShellProvider shellProvider)
{
    public List<string> ObterPerfis() => shellProvider.ObterPerfisTerminal();
}

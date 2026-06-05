using System.Diagnostics;

namespace ProjectManagerWeb.src.Utils.Terminais;

public class PtyxisTerminal : ITerminalEmulator
{
    public void Executar(string command, string? perfilTerminal = null)
    {
        var trimmed = command.TrimEnd(' ', ';');
        string args;

        if (!string.IsNullOrWhiteSpace(perfilTerminal))
        {
            var uuid = ResolverUuidPerfil(perfilTerminal);
            args = $"--tab-with-profile=\"{uuid}\" -- bash -l -i -c \"trap '' INT; {EscapeBash(trimmed)}; exec bash\"";
        }
        else
        {
            args = $"--tab -- bash -l -i -c \"trap '' INT; {EscapeBash(trimmed)}; exec bash\"";
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "ptyxis",
            Arguments = args,
            UseShellExecute = false
        });
    }

    public List<string> ObterPerfis()
    {
        try
        {
            var uuidsRaw = ExecutarProcesso("dconf", "read /org/gnome/Ptyxis/profile-uuids");
            if (string.IsNullOrWhiteSpace(uuidsRaw)) return [];

            var uuids = uuidsRaw
                .Trim('[', ']', '\n', ' ')
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim().Trim('\''))
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .ToList();

            var perfis = new List<string>();

            foreach (var uuid in uuids)
            {
                var label = ExecutarProcesso("dconf", $"read /org/gnome/Ptyxis/Profiles/{uuid}/label");
                var nome = label?.Trim().Trim('\'');
                perfis.Add(!string.IsNullOrWhiteSpace(nome) ? nome : uuid);
            }

            return perfis;
        }
        catch
        {
            return [];
        }
    }

    private static string ResolverUuidPerfil(string perfilTerminal)
    {
        var uuidsRaw = ExecutarProcesso("dconf", "read /org/gnome/Ptyxis/profile-uuids");
        if (string.IsNullOrWhiteSpace(uuidsRaw)) return perfilTerminal;

        var uuids = uuidsRaw
            .Trim('[', ']', '\n', ' ')
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim().Trim('\''))
            .Where(u => !string.IsNullOrWhiteSpace(u));

        foreach (var uuid in uuids)
        {
            var label = ExecutarProcesso("dconf", $"read /org/gnome/Ptyxis/Profiles/{uuid}/label");
            if (label?.Trim().Trim('\'') == perfilTerminal)
                return uuid;
        }

        return perfilTerminal;
    }

    private static string ExecutarProcesso(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null) return string.Empty;

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(3000);
        return output;
    }

    private static string EscapeBash(string command) =>
        command.Replace("\"", "\\\"");
}

using System.Text.Json;

namespace ProjectManagerWeb.src.Services;

public class TerminalService
{
    public List<string> ObterPerfis()
    {
        var settingsPath = ObterCaminhoSettings();
        if (settingsPath is null || !File.Exists(settingsPath))
            return [];

        try
        {
            var json = File.ReadAllText(settingsPath);
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            if (!doc.RootElement.TryGetProperty("profiles", out var profiles))
                return [];

            if (!profiles.TryGetProperty("list", out var list))
                return [];

            return list.EnumerateArray()
                .Where(p => !p.TryGetProperty("hidden", out var h) || !h.GetBoolean())
                .Where(p => p.TryGetProperty("name", out _))
                .Select(p => p.GetProperty("name").GetString()!)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string? ObterCaminhoSettings()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Windows Terminal (Store)
        var storePath = Path.Combine(localAppData, "Packages", "Microsoft.WindowsTerminal_8wekyb3d8bbwe", "LocalState", "settings.json");
        if (File.Exists(storePath)) return storePath;

        // Windows Terminal (não-Store / scoop / winget)
        var nonStorePath = Path.Combine(localAppData, "Microsoft", "Windows Terminal", "settings.json");
        if (File.Exists(nonStorePath)) return nonStorePath;

        // Windows Terminal Preview (Store)
        var previewPath = Path.Combine(localAppData, "Packages", "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe", "LocalState", "settings.json");
        if (File.Exists(previewPath)) return previewPath;

        return null;
    }
}

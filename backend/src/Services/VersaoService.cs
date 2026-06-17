using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

public class VersaoService(HttpClient httpClient)
{
    private const string RepoUrl = "wallaceSW11/ProjectManagerWeb";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private static DateTime _ultimaConsulta = DateTime.MinValue;
    private static VersaoResponseDTO? _cache;

    public async Task<VersaoResponseDTO> ObterVersaoAsync()
    {
        var versaoAtual = ObterVersaoAtual();

        if (_cache is not null && DateTime.UtcNow - _ultimaConsulta < CacheDuration)
            return _cache with { VersaoAtual = versaoAtual };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/repos/{RepoUrl}/releases/latest");

            request.Headers.UserAgent.ParseAdd("PMW-Updater");

            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new VersaoResponseDTO(versaoAtual, null, null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tag) ? tag.GetString() : null;
            var htmlUrl = root.TryGetProperty("html_url", out var url) ? url.GetString() : null;

            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Linux";
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (name is not null && name.Contains($"PMW_{os}_", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.TryGetProperty("browser_download_url", out var d)
                            ? d.GetString()
                            : null;
                        break;
                    }
                }
            }

            var versaoNova = tagName?.TrimStart('v') ?? null;
            if (versaoNova is not null && CompararVersao(versaoNova, versaoAtual) <= 0)
                versaoNova = null;

            var resultado = new VersaoResponseDTO(versaoAtual, versaoNova, htmlUrl, downloadUrl);

            _cache = resultado;
            _ultimaConsulta = DateTime.UtcNow;

            return resultado;
        }
        catch
        {
            return new VersaoResponseDTO(versaoAtual, null, null, null);
        }
    }

    public static void AtualizarAplicacao()
    {
        if (OperatingSystem.IsWindows())
            ExecutarAtualizacaoWindows();
        else
            ExecutarAtualizacaoLinux();
    }

    private static void ExecutarAtualizacaoWindows()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c start \"PMW Update\" pwsh.exe -NoExit -Command \"pmw update\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(psi);
    }

    private static void ExecutarAtualizacaoLinux()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = "-c \"nohup bash -c 'pmw update' > /tmp/pmw-update.log 2>&1 &\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(psi);
    }

    private static string ObterVersaoAtual()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (infoVersion is not null)
        {
            var match = System.Text.RegularExpressions.Regex.Match(infoVersion, @"\d+\.\d+\.\d+");
            if (match.Success) return match.Value;
        }
        var version = assembly.GetName().Version;
        return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    internal static int CompararVersao(string a, string b)
    {
        var partesA = a.Split('.');
        var partesB = b.Split('.');

        for (int i = 0; i < Math.Max(partesA.Length, partesB.Length); i++)
        {
            int numA = i < partesA.Length && int.TryParse(partesA[i], out var x) ? x : 0;
            int numB = i < partesB.Length && int.TryParse(partesB[i], out var y) ? y : 0;

            if (numA != numB)
                return numA.CompareTo(numB);
        }

        return 0;
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services.MeuQuerido;

public class OpenCodeClient(IHttpClientFactory httpClientFactory, OpenCodeDescoberta descoberta) : IOpenCodeClient
{
    private const string Usuario = "opencode";
    private static readonly TimeSpan TimeoutConsulta = TimeSpan.FromSeconds(3);

    public async Task<StatusOpenCodeDTO> ObterStatusAsync(CancellationToken ct = default)
    {
        var resultado = await descoberta.DescobrirAsync(ct);

        if (!resultado.Sucesso)
            return Indisponivel(resultado.Motivo);

        try
        {
            var versao = await ConsultarVersaoAsync(resultado.Url!, resultado.Senha!, ct);

            if (versao is null)
                return Indisponivel($"Resposta inválida de {resultado.Url}/api/info", resultado.Url);

            if (!VersaoCompativel(versao))
                return Indisponivel(
                    $"Versão do OpenCode incompatível: {versao}. O Meu Querido espera OpenCode 2.x.",
                    resultado.Url);

            return new StatusOpenCodeDTO(true, versao, resultado.Url, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Indisponivel(
                $"Serviço OpenCode não respondeu em {TimeoutConsulta.TotalSeconds}s",
                resultado.Url);
        }
        catch (HttpRequestException ex)
        {
            return Indisponivel(ex.Message, resultado.Url);
        }
        catch (IOException)
        {
            return Indisponivel($"Serviço OpenCode não respondeu em {resultado.Url}", resultado.Url);
        }
    }

    private async Task<string?> ConsultarVersaoAsync(string url, string senha, CancellationToken ct)
    {
        using var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeoutConsulta;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Credencial(senha));

        using var resposta = await httpClient.GetAsync($"{url}/api/info", ct);

        if (resposta.StatusCode == HttpStatusCode.Unauthorized)
            throw new HttpRequestException("Autenticação recusada pelo serviço OpenCode");

        if (!resposta.IsSuccessStatusCode)
            throw new HttpRequestException($"Serviço OpenCode respondeu {(int)resposta.StatusCode}");

        var json = await resposta.Content.ReadAsStringAsync(ct);

        try
        {
            using var documento = JsonDocument.Parse(json);

            return documento.RootElement.TryGetProperty("version", out var valor)
                && valor.ValueKind == JsonValueKind.String
                    ? valor.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool VersaoCompativel(string versao) =>
        versao.StartsWith("2.", StringComparison.Ordinal);

    private static string Credencial(string senha) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Usuario}:{senha}"));

    private static StatusOpenCodeDTO Indisponivel(string? motivo, string? url = null) =>
        new(false, null, url, motivo);
}

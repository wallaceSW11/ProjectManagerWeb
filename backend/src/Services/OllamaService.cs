using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ProjectManagerWeb.src.DTOs;

namespace ProjectManagerWeb.src.Services;

/// <summary>
/// Implementação do ILLMService para Ollama local.
/// URL padrão: http://localhost:11434
/// Modelo padrão: qwen2.5:7b (bom em português + function calling)
/// </summary>
public class OllamaService : ILLMService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _modelo;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public OllamaService(IConfiguration config)
    {
        _baseUrl = config.GetValue<string>("Ollama:BaseUrl") ?? "http://localhost:11434";
        _modelo = config.GetValue<string>("Ollama:Modelo") ?? "qwen2.5:7b";
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
    }

    public async Task<LLMRespostaDTO> EnviarMensagemAsync(LLMRequestDTO request, CancellationToken ct = default)
    {
        var mensagens = new List<object>
        {
            new { role = "system", content = request.PromptSistema }
        };

        foreach (var msg in request.Historico)
            mensagens.Add(new { role = msg.Role, content = msg.Conteudo });

        var tools = request.Tools.Select(t => new
        {
            type = "function",
            function = new
            {
                name = t.Nome,
                description = t.Descricao,
                parameters = t.Parametros
            }
        });

        var payload = new
        {
            model = _modelo,
            messages = mensagens,
            tools,
            stream = false
        };

        var json = JsonSerializer.Serialize(payload, _jsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync($"{_baseUrl}/api/chat", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException("O JARVAS demorou demais pra pensar. Tenta de novo.");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Não consegui falar com o Ollama. Verifique se ele está rodando.", ex);
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        return ParseResposta(responseJson);
    }

    public async Task<bool> VerificarDisponibilidadeAsync()
    {
        try
        {
            var resp = await _http.GetAsync($"{_baseUrl}/api/tags");
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task WarmUpAsync()
    {
        try
        {
            // Manda uma mensagem mínima só pra carregar o modelo na memória
            var payload = new { model = _modelo, messages = new[] { new { role = "user", content = "oi" } }, stream = false };
            var json = JsonSerializer.Serialize(payload, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _http.PostAsync($"{_baseUrl}/api/chat", content);
        }
        catch
        {
            // Silencioso — se falhar no warm-up não é crítico
        }
    }

    private LLMRespostaDTO ParseResposta(string json)
    {
        try
        {
            var node = JsonNode.Parse(json);
            var message = node?["message"];

            // Verifica se tem tool_calls
            var toolCalls = message?["tool_calls"]?.AsArray();
            if (toolCalls != null && toolCalls.Count > 0)
            {
                var tool = toolCalls[0];
                var nome = tool?["function"]?["name"]?.GetValue<string>();
                var args = tool?["function"]?["arguments"]?.ToJsonString();

                return new LLMRespostaDTO(
                    EhToolCall: true,
                    ToolNome: nome,
                    ToolArgumentosJson: args,
                    TextoResposta: null
                );
            }

            // Resposta de texto normal
            var texto = message?["content"]?.GetValue<string>() ?? "Não entendi. Pode reformular?";
            return new LLMRespostaDTO(
                EhToolCall: false,
                ToolNome: null,
                ToolArgumentosJson: null,
                TextoResposta: texto
            );
        }
        catch
        {
            return new LLMRespostaDTO(
                EhToolCall: false,
                ToolNome: null,
                ToolArgumentosJson: null,
                TextoResposta: "Recebi uma resposta que não consegui entender. Tenta de novo."
            );
        }
    }
}

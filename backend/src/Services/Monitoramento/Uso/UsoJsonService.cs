using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectManagerWeb.src.DTOs;
using ProjectManagerWeb.src.Utils;

namespace ProjectManagerWeb.src.Services.Monitoramento.Uso;

public class UsoJsonService
{
    private const string PrefixoArquivo = "uso-";
    private const string ExtensaoArquivo = ".jsonl";
    private const string FormatoData = "yyyy-MM-dd";
    private const string NomeDiretorioUso = "uso";
    private const string NomeDiretorioRelatorios = "relatorios";

    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly Encoding _codificacao = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILogger<UsoJsonService> _logger;
    private readonly string _diretorioUso;

    private DateOnly? _ultimoDiaVerificado;
    private readonly HashSet<DateOnly> _diasComMaquina = [];

    public UsoJsonService(ILogger<UsoJsonService> logger)
        : this(logger, Path.Combine(PathHelper.BancoPath, NomeDiretorioUso))
    {
    }

    internal UsoJsonService(ILogger<UsoJsonService> logger, string diretorioUso)
    {
        _logger = logger;
        _diretorioUso = diretorioUso;
    }

    public string DiretorioUso => _diretorioUso;

    public string DiretorioRelatorios => Path.Combine(_diretorioUso, NomeDiretorioRelatorios);

    public async Task GravarLoteAsync(
        IReadOnlyList<UsoAmostraDTO> lote,
        MaquinaDTO? maquina,
        int retencaoDias,
        CancellationToken ct)
    {
        if (lote.Count == 0) return;

        await _semaphore.WaitAsync(ct);
        try
        {
            foreach (var grupo in lote.GroupBy(amostra => DateOnly.FromDateTime(amostra.Ts.LocalDateTime)))
                await GravarDiaAsync(grupo.Key, [.. grupo], maquina, ct);

            VerificarRetencao(retencaoDias);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public IEnumerable<DateOnly> ListarDias()
    {
        if (!Directory.Exists(_diretorioUso)) return [];

        var dias = new List<DateOnly>();
        foreach (var arquivo in Directory.GetFiles(_diretorioUso, $"{PrefixoArquivo}*{ExtensaoArquivo}"))
        {
            var nome = Path.GetFileNameWithoutExtension(arquivo)[PrefixoArquivo.Length..];
            if (DateOnly.TryParseExact(nome, FormatoData, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
                dias.Add(dia);
        }

        return [.. dias.OrderBy(dia => dia)];
    }

    public async Task<List<UsoAmostraDTO>> LerDiaAsync(DateOnly dia, CancellationToken ct)
    {
        var caminho = CaminhoDoDia(dia);
        if (!File.Exists(caminho)) return [];

        var amostras = new List<UsoAmostraDTO>();
        var invalidas = 0;

        using var leitor = new StreamReader(caminho, _codificacao);
        while (await leitor.ReadLineAsync(ct) is { } linha)
        {
            if (string.IsNullOrWhiteSpace(linha)) continue;

            try
            {
                var amostra = JsonSerializer.Deserialize<UsoAmostraDTO>(linha, _jsonOptions);
                if (amostra is not null)
                    amostras.Add(amostra);
            }
            catch (JsonException)
            {
                invalidas++;
            }
        }

        if (invalidas > 0)
            _logger.LogWarning("Relatório de uso: {Invalidas} linha(s) inválida(s) ignorada(s) em {Dia}", invalidas, dia);

        return amostras;
    }

    private async Task GravarDiaAsync(
        DateOnly dia,
        IReadOnlyList<UsoAmostraDTO> amostras,
        MaquinaDTO? maquina,
        CancellationToken ct)
    {
        Directory.CreateDirectory(_diretorioUso);

        var caminho = CaminhoDoDia(dia);
        var incluirMaquina = maquina is not null && await PrecisaGravarMaquinaAsync(dia, caminho, ct);

        var conteudo = new StringBuilder();
        for (var i = 0; i < amostras.Count; i++)
        {
            var amostra = i == 0 && incluirMaquina
                ? amostras[i] with { Maquina = maquina }
                : amostras[i];

            conteudo.Append(JsonSerializer.Serialize(amostra, _jsonOptions)).Append('\n');
        }

        await File.AppendAllTextAsync(caminho, conteudo.ToString(), _codificacao, ct);

        if (incluirMaquina)
            _diasComMaquina.Add(dia);
    }

    private async Task<bool> PrecisaGravarMaquinaAsync(DateOnly dia, string caminho, CancellationToken ct)
    {
        if (_diasComMaquina.Contains(dia)) return false;
        if (!File.Exists(caminho)) return true;

        using var leitor = new StreamReader(caminho, _codificacao);
        var primeiraLinha = await leitor.ReadLineAsync(ct);

        if (primeiraLinha?.Contains("\"maquina\"", StringComparison.Ordinal) == true)
        {
            _diasComMaquina.Add(dia);
            return false;
        }

        return true;
    }

    private void VerificarRetencao(int retencaoDias)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        if (_ultimoDiaVerificado == hoje) return;

        _ultimoDiaVerificado = hoje;
        if (retencaoDias <= 0 || !Directory.Exists(_diretorioUso)) return;

        var limite = hoje.AddDays(-retencaoDias);
        foreach (var arquivo in Directory.GetFiles(_diretorioUso, $"{PrefixoArquivo}*{ExtensaoArquivo}"))
        {
            var nome = Path.GetFileNameWithoutExtension(arquivo)[PrefixoArquivo.Length..];
            var valido = DateOnly.TryParseExact(
                nome,
                FormatoData,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dia);

            if (valido && dia < limite)
                File.Delete(arquivo);
        }
    }

    private string CaminhoDoDia(DateOnly dia) =>
        Path.Combine(_diretorioUso, $"{PrefixoArquivo}{dia.ToString(FormatoData, CultureInfo.InvariantCulture)}{ExtensaoArquivo}");
}

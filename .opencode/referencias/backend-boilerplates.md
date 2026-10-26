# Boilerplates — Backend .NET 9

Referência de código real do PMW. As regras estão em `.opencode/code-style/backend.md`.

## Controller

```csharp
[ApiController]
[Route("api/repositorios")]
public class RepositorioController(RepositorioJsonService repositorioService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ConsultarTodos() =>
        Ok(await repositorioService.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] RepositorioRequestDTO dto)
    {
        if (dto is null) return BadRequest("O corpo da requisição não pode ser nulo.");
        try
        {
            var cadastrado = await repositorioService.AddAsync(dto);
            return Ok(cadastrado);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
```

## JsonService

```csharp
public class XJsonService
{
    private static readonly string FilePath = Path.Combine(PathHelper.BancoPath, "x.json");
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public XJsonService()
    {
        if (!Directory.Exists(PathHelper.BancoPath))
            Directory.CreateDirectory(PathHelper.BancoPath);
    }

    public async Task<List<XDto>> GetAllAsync() =>
        await LerListaDoArquivoAsync();

    public async Task<XDto> AddAsync(XDto novo)
    {
        await _semaphore.WaitAsync();
        try
        {
            var lista = await LerListaDoArquivoAsync(locked: true);
            lista.Add(novo);
            await GravarListaNoArquivoAsync(lista, locked: true);
            return novo;
        }
        finally { _semaphore.Release(); }
    }

    private static async Task<List<XDto>> LerListaDoArquivoAsync(bool locked = false)
    {
        if (!locked) await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(FilePath)) return [];
            var json = await File.ReadAllTextAsync(FilePath);
            return string.IsNullOrWhiteSpace(json)
                ? []
                : JsonSerializer.Deserialize<List<XDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        finally { if (!locked) _semaphore.Release(); }
    }

    private async Task GravarListaNoArquivoAsync(List<XDto> lista, bool locked = false)
    {
        if (!locked) await _semaphore.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(lista, _jsonOptions));
        }
        finally { if (!locked) _semaphore.Release(); }
    }
}
```

## Remover ReadOnly antes de excluir diretório (Windows)

```csharp
private static void RemoverAtributosReadOnly(DirectoryInfo diretorio)
{
    foreach (var arquivo in diretorio.GetFiles("*", SearchOption.AllDirectories))
        arquivo.Attributes = FileAttributes.Normal;
    foreach (var subDiretorio in diretorio.GetDirectories("*", SearchOption.AllDirectories))
        subDiretorio.Attributes = FileAttributes.Normal;
}
```

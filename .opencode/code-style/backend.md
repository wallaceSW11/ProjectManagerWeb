# Code Style — Backend .NET 9 C#

## Proibido

- AutoMapper — mapeamento manual sempre
- Expor entidade na response — use DTO
- Lógica de negócio no controller
- `.Result` ou `.Wait()` — async/await sempre
- Engolir exceção (catch vazio) — sempre throw (exceto fallbacks intencionais de IIS/shell)
- Construtor tradicional quando primary constructor resolve
- Acessar arquivo JSON direto no controller — sempre via JsonService

## Controle de fluxo

Early return. Sem ifs aninhados. Ternário só pra 2 caminhos.

```csharp
// errado
if (item != null) { if (item.Identificador == id) { return Ok(item); } }

// certo
if (item is null || item.Identificador != id)
    return NotFound();
return Ok(item);
```

## Async

Sempre async/await. Sufixo `Async` em métodos assíncronos.

## Controllers

Finos — só orquestração. `ControllerBase`. Primary constructor.
`IActionRole` em todos os retornos (sem `ActionResult<T>`).

```csharp
[ApiController]
[Route("api/repositorios")]
public class RepositorioController(RepositorioJsonService repositorioService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionRole> ConsultarTodos() =>
        Ok(await repositorioService.GetAllAsync());

    [HttpPost]
    public async Task<IActionRole> Cadastrar([FromBody] RepositorioRequestDTO dto)
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

## Json Services

Padrão obrigatório:
- `SemaphoreSlim(1,1)` pra concorrência
- `LerListaDoArquivoAsync(bool locked)` e `GravarListaNoArquivoAsync(lista, bool locked)`
- `locked: true` quando já dentro do semáforo

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

## DTOs

```
XRequestDTO  → input (POST/PUT)
XResponseDTO → response quando difere do request
XDTO         → quando serve pra ambos (padrão do projeto)
```

## Nomenclatura

PascalCase: classes, interfaces, constantes.
Prefixo `I`: interfaces.
Sufixo `Async`: métodos assíncronos.
camelCase: parâmetros e variáveis.
Nomes em português: `ObterTodas`, `Cadastrar`, `Clonar`.

## Shell / processos externos

Usar `ShellExecute.ExecutarComando()` ou `ProcessStartInfo`.
IIS: `Verb = "runas"`. Tratar `TimeoutException` com fallback.

## Exclusão de diretórios no Windows

`Directory.Delete` falha com `Access Denied` em arquivos `ReadOnly`. Remover atributos antes:

```csharp
private static void RemoverAtributosReadOnly(DirectoryInfo diretorio)
{
    foreach (var arquivo in diretorio.GetFiles("*", SearchOption.AllDirectories))
        arquivo.Attributes = FileAttributes.Normal;
    foreach (var subDiretorio in diretorio.GetDirectories("*", SearchOption.AllDirectories))
        subDiretorio.Attributes = FileAttributes.Normal;
}
```

## Upsert em Json Services

`PastaJsonService.AddAsync`: upsert (deleta e recria se mesmo diretório)
`RepositorioJsonService.AddAsync`: lança exceção se URL duplicada
`IDEJsonService.AddAsync`: lança exceção se nome duplicado

Sempre defina explicitamente o comportamento ao criar novo JsonService.

## Migrations

`MigrationService`. Sem `dotnet ef`.
Nova migration: `Migration_00X_Nome()` + registrar em `ExecuteMigrationsAsync()`.
Scripts manuais em `backend/migrations/`.

## Camadas

```
Controller → Service/JsonService → arquivo JSON
RequestDTO → Service → ResponseDTO
```

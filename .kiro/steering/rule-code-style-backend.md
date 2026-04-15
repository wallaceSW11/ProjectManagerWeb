---
inclusion: fileMatch
fileMatchPattern: "backend/**"
---

# rule-code-style-backend

Regras de código para o backend .NET 9 C#. Sem exceções.

## contexto do projeto

Sem EF Core. Sem banco relacional. Sem AutoMapper. Sem FluentValidation. Sem JWT.
Persistência via arquivos JSON em `%APPDATA%\PMW\Banco\` — cada entidade tem seu próprio `XJsonService`.

## proibido

- NUNCA AutoMapper — mapeamento manual no service ou no próprio DTO
- NUNCA expor entidade na response — sempre DTO
- NUNCA lógica de negócio no controller
- NUNCA .Result ou .Wait() — sempre async/await
- NUNCA engolir exceção — sempre throw (exceto em fallbacks de IIS/shell onde silenciar é intencional)
- NUNCA construtor tradicional quando primary constructor resolve
- NUNCA acessar arquivo JSON diretamente no controller — sempre via JsonService

## controle de fluxo

Early return sempre. Sem ifs aninhados. Ternário só para 2 caminhos simples.

```csharp
// errado
if (item != null) { if (item.Identificador == id) { return Ok(item); } }

// certo
if (item is null || item.Identificador != id)
    return NotFound();
return Ok(item);
```

## async

Sempre async/await. Todo método assíncrono: sufixo `Async`.

## controllers

Finos — só orquestração. Herdam `ControllerBase`. Primary constructor.
`IActionResult` em todos os retornos (sem `ActionResult<T>` — padrão atual do projeto).

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

## json services

Responsáveis pela persistência em arquivo JSON. Padrão obrigatório:
- `SemaphoreSlim(1,1)` para controle de concorrência
- Métodos privados `LerListaDoArquivoAsync(bool locked)` e `GravarListaNoArquivoAsync(..., bool locked)`
- `locked: true` quando já dentro do semáforo (evita deadlock)
- Inicializar diretório no construtor se não existir

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

```csharp
// records para DTOs simples
public record IDEDTO(Guid Identificador, string Nome, string Comando, bool AceitaPerfilPersonalizado);

// classes para DTOs com lógica ou herança
public class RepositorioRequestDTO { ... }
```

Sufixos:
- `XRequestDTO` — input (POST/PUT)
- `XResponseDTO` — response quando diferente do request
- `XDTO` — quando serve para ambos (padrão atual do projeto)

## nomenclatura

PascalCase: classes, interfaces, constantes.
Prefixo `I`: interfaces.
Sufixo `Async`: métodos assíncronos.
camelCase: parâmetros e variáveis.
Nomes em português são aceitos neste projeto (ex: `ObterTodas`, `Cadastrar`, `Clonar`).

## shell / processos externos

Comandos externos (git, dotnet publish, appcmd) são executados via `ShellExecute.ExecutarComando()` ou `ProcessStartInfo`.
Operações IIS requerem `Verb = "runas"` — sempre tratar `TimeoutException` e fallback.

## exclusão de diretórios no Windows

`Directory.Delete(path, recursive: true)` falha com `Access Denied` em arquivos com atributo `ReadOnly` (comum em pastas `.git`). Sempre remover atributos antes de deletar:

```csharp
private static void RemoverAtributosReadOnly(DirectoryInfo diretorio)
{
    foreach (var arquivo in diretorio.GetFiles("*", SearchOption.AllDirectories))
        arquivo.Attributes = FileAttributes.Normal;

    foreach (var subDiretorio in diretorio.GetDirectories("*", SearchOption.AllDirectories))
        subDiretorio.Attributes = FileAttributes.Normal;
}

// uso:
RemoverAtributosReadOnly(new DirectoryInfo(caminho));
Directory.Delete(caminho, recursive: true);
```

## upsert em json services — comportamento de AddAsync

Alguns `JsonService` implementam **upsert silencioso** em vez de rejeitar duplicatas.
Antes de implementar um novo `AddAsync`, verificar qual comportamento é esperado para a entidade.

- `PastaJsonService.AddAsync`: se já existe entrada com o mesmo `Diretorio`, **deleta e recria** — não lança exceção. Necessário porque o clone sempre recria a pasta.
- `RepositorioJsonService.AddAsync`: lança exceção se URL duplicada.
- `IDEJsonService.AddAsync`: lança exceção se nome duplicado.

Padrão a seguir ao criar novo JsonService: definir explicitamente se é upsert ou rejeição, e documentar no próprio método.

## migrations

Sistema próprio em `MigrationService`. Sem `dotnet ef`.
Nova migration: adicionar `Migration_00X_Nome()` + registrar em `ExecuteMigrationsAsync()`.
Scripts PowerShell manuais ficam em `backend/migrations/`.

## camadas

```
Controller → Service/JsonService → arquivo JSON
Request DTO → Service → Response DTO
```

Controller não acessa arquivo JSON diretamente.

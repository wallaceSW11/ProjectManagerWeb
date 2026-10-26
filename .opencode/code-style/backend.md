# Code Style — Backend .NET 9 C#

Boilerplates completos (controller, JsonService, exclusão no Windows): `.opencode/referencias/backend-boilerplates.md`.
Leia o boilerplate antes de criar um arquivo novo.

## Proibido

- Comentários no código — nomes auto-documentados. Zero comentários, inclusive de seção/bloco.
- If de uma linha COM chaves `{}` — early return incluso.
  ✅ `if (item is null) return NotFound();`
  ❌ `if (item is null) { return NotFound(); }`
- If/else — use ternário pra 2 caminhos ou objeto mapeado pra múltiplos.
- AutoMapper — mapeamento manual sempre.
- Expor entidade na response — use DTO.
- Lógica de negócio no controller.
- `.Result` ou `.Wait()` — async/await sempre.
- Engolir exceção (catch vazio) — sempre throw (exceto fallback intencional de IIS/shell).
- Construtor tradicional quando primary constructor resolve.
- Acessar arquivo JSON direto no controller — sempre via JsonService.

## Controle de fluxo

Early return. Sem ifs aninhados. Ternário só pra 2 caminhos.

```csharp
if (item is null || item.Identificador != id)
    return NotFound();
return Ok(item);
```

## Async

Sempre async/await. Sufixo `Async` em métodos assíncronos.

## Controllers

Finos — só orquestração. `ControllerBase`. Primary constructor.
`IActionResult` em todos os retornos (sem `ActionResult<T>`).

## Json Services

Padrão obrigatório:
- `SemaphoreSlim(1,1)` pra concorrência
- `LerListaDoArquivoAsync(bool locked)` e `GravarListaNoArquivoAsync(lista, bool locked)`
- `locked: true` quando já dentro do semáforo

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

`ShellExecute.ExecutarComando()` ou `ProcessStartInfo`.
IIS: `Verb = "runas"`. Tratar `TimeoutException` com fallback.

## Exclusão de diretórios no Windows

`Directory.Delete` falha com `Access Denied` em arquivo `ReadOnly` (comum em `.git`).
Remover atributos antes — código em `.opencode/referencias/backend-boilerplates.md`.

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

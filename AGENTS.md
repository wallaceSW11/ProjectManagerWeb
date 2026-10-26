# ProjectManagerWeb (PMW)

Gerenciador local de repositórios Git, pastas de projetos, IDEs e deploy IIS.
Stack, domínio, persistência, migrations e infraestrutura: `.opencode/ecosystem-pmw.md`.

## Estrutura

```
frontend/   Vue 3 + TypeScript + Vuetify 3 + Pinia + Vite + pnpm
backend/    .NET 9 + ASP.NET Core (sem EF, sem AutoMapper)
.opencode/  agentes, skills, flows e code style do OpenCode
```

## Comandos

```bash
# frontend/
pnpm install
pnpm run dev        # localhost:5173
pnpm run build
pnpm run format     # prettier — rodar antes de entregar
pnpm run lint

# backend/
dotnet build
dotnet run          # localhost:2024
dotnet publish
```

Testes: `dotnet test backend/tests/ProjectManagerWeb.Tests/ProjectManagerWeb.Tests.csproj` — somente via skill `pipeline`.

## Code style

Sempre carregados: `.opencode/code-style/backend.md` e `.opencode/code-style/frontend.md`.
O essencial:

- Sem comentários no código — nomes auto-documentados
- If de uma linha SEM chaves: `if (!item) return;`
- Evite if/else — ternário pra 2 caminhos, objeto mapeado pra vários
- Camadas: Component → Store → Service → API | Controller → Service → JsonService

## Proibições

- NUNCA hardcodar secrets, API keys ou connection strings
- **NUNCA git add, commit, push, pull, merge, rebase.** O usuário revisa e commita pessoalmente. Exceção: skills `criar-mr` e `pipeline`.
- NUNCA inventar informação — investigue o fonte antes de responder
- **NUNCA criar ou rodar testes unitários.** Ciclos separados: implemento, você valida manualmente, depois roda a skill de testes. Exceção: `pipeline`.

## Git

Branch de release: `main`. Trabalho em `PMW<n>`.
Commits: `tipo(escopo): descrição em pt-br` — feat, fix, chore, refactor, style, docs, test.
PR sempre contra `main`. Commits atômicos. Sem secrets no commit.

### Pós-merge

Cada PR mergeada na `main` gera commit de bump de versão. Para sincronizar a branch local:

```bash
git checkout <sua-branch>
git pull origin main
```

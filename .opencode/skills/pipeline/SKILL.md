---
name: pipeline
description: Pipeline completa: diff com main, análise de testes, validação de code style, criação de testes se necessário, commit e PR.
---

# Skill: pipeline (`/pipeline`)

Fluxo completo de entrega: diff → análise → testes → code review → MR.

## Comportamento

- Tudo em português, seja direto sem enrolação
- **Fluxo 100% automático** — não pergunte nada ao usuário (nem mensagem de commit, nem confirmação)
- Autorizado a git add, commit, push e gh pr create
- Autorizado a criar, modificar e rodar testes unitários (`dotnet test`)
- **NUNCA** rode o script `test-coverage.sh` (só `dotnet test`)

## Workflow

### 1. Análise do diff

```bash
git fetch origin main
git log main..HEAD --oneline --no-decorate
git diff main..HEAD --stat
git diff main..HEAD -- '*.cs' '*.vue' '*.ts' | head -500
```

Extraia a lista de arquivos modificados e avalie:
- Novos arquivos `.cs` (controllers, services, DTOs, utils)
- Modificações em arquivos `.cs` existentes
- Arquivos `.vue` e `.ts` do frontend
- Arquivos de teste existentes em `backend/tests/ProjectManagerWeb.Tests/`

### 2. Verificar necessidade de testes

Para cada arquivo `.cs` no backend que foi:
- **Criado**: verificar se existe teste correspondente em `backend/tests/ProjectManagerWeb.Tests/`. Se não existir, é necessária a criação.
- **Modificado**: verificar se os métodos alterados/novos têm cobertura nos testes existentes. Se faltar cobertura, os testes precisam ser atualizados.

Regra: siga o padrão `NomeClasseTests.cs` já existente nos testes.

### 3. Validar code style

Percorrer os arquivos modificados e verificar:

**Backend (.cs):**
- Sem AutoMapper (`AutoMapper`, `CreateMap`, `IMapper`)
- Sem `.Result` / `.Wait()` — deve ser async/await
- Sem `Console.WriteLine` (exceto `Program.cs`)
- Sem catch vazio — sempre tratar ou relançar
- Controller sem lógica de negócio — delegar ao Service
- Usar `IActionResult`, não `ActionResult<T>`
- If de uma linha sem chaves — early return
- Sem if/else aninhados — ternário ou early return
- Primary constructor quando possível
- Async/await com sufixo `Async`

**Frontend (.vue / .ts):**
- Sem `.then().catch()` — async/await
- Sem `var` — const/let
- Sem `console.log`
- Sem `!important` no CSS
- Loading reseta no `finally`, nunca no try/catch
- Component → Store → Service (nunca chamar service direto do component)
- If de uma linha sem chaves — early return
- Evitar if/else — ternário pra 2 caminhos ou objetos mapeados

Se encontrar violação, **corrija imediatamente** antes de prosseguir.

### 4. Criar/atualizar testes (se necessário)

Se na etapa 2 foi identificada necessidade de testes:

**A)** Para CADA arquivo `.cs` criado no backend sem teste correspondente:
  - Usar o subagent `@testador` com instruções detalhadas sobre qual classe testar
  - Seguir o padrão dos testes existentes: xUnit + FluentAssertions + NSubstitute
  - Criar o teste em `backend/tests/ProjectManagerWeb.Tests/Services/` (ou Utils/)
  - Nome do arquivo: `NomeClasseTests.cs`
  - Estrutura: classe `NomeClasseTests` com subclasse para cada método `NomeMetodo`

**B)** Para métodos novos/modificados sem cobertura:
  - Usar o subagent `@testador` para adicionar testes específicos aos arquivos existentes

**C)** Após criar/atualizar todos os testes:
```bash
dotnet test backend/tests/ProjectManagerWeb.Tests/ProjectManagerWeb.Tests.csproj --no-restore
```

Se os testes falharem, corrija e repita até passarem.

### 5. Verificar estado final

```bash
git status
git diff --stat
```

Confirmar que:
- Todos os arquivos seguem code style
- Testes passam (se foram criados/alterados)
- Dependências/imports não quebrados

### 6. Commit (se houver mudanças)

Se houver arquivos não commitados (correções de code style + testes):

Analise o diff completo para gerar a mensagem de commit automaticamente:
- Use `git diff --cached --stat` + `git diff --cached` para entender as mudanças
- Extraia o tipo do escopo dos arquivos alterados (feat, fix, refactor, style, test, chore)
- Descrição curta e direta em português
- Formato: `tipo(escopo): descrição em pt-br`

```bash
git add -A
git commit -m "tipo(escopo): descrição"
```

### 7. Push e criar MR

```bash
git push origin <branch-atual>
```

Use `git log main..HEAD` para obter os commits e `git diff --stat main..HEAD` para o resumo das mudanças. Gere o título do PR a partir do commit message.

Criar PR com `gh pr create` preenchendo o template automaticamente:

```markdown
## O que mudou

### `{arquivos principais}`
{descrição concisa das mudanças}

### Testes
{se houver, listar novos testes criados}

## Como testar
{dotnet test ou passos}
```

Título do PR: `tipo(escopo): descrição` (igual ao commit)

### 8. Retornar a URL do PR

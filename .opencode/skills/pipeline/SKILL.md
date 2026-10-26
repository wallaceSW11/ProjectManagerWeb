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

### 0. Configurar git do usuário

Antes de qualquer operação git, execute o comando para trocar o gh para o usuário pessoal:

```bash
gh auth switch --user wallaceSW11
```

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

### 2. Decidir se precisa de teste

**Arquivo novo não é motivo para teste.** A pergunta é: existe regra de negócio para proteger?

| Testar sempre | Não testar nunca |
|---|---|
| Cálculo, agregação, conversão **com regra** | DTO, record, model de dados — preencher campo não é lógica |
| Parser de arquivo, saída de comando, formato externo | Enum, constante, interface |
| Ramificação e tratamento de erro | Registro de DI, configuração, options |
| Fronteira de regra (`>=` vs `>`, retenção, rotação, intervalo) | Mapeamento trivial campo→campo, sem regra |
| Fallback e normalização de dado legado | Sanidade do ambiente (`> 0`, `<= ProcessorCount`) |
| | Serialização JSON, qualquer coisa de framework |

Exceção: um mapeamento pode ser teste se tiver regra dentro (ex.: `usada = total - livre` quando o campo novo falta) — aí o que se testa é a regra, não o mapeamento.

**Critério de valor:** se este teste fosse removido, qual bug de negócio deixaria de ser pego? Se a resposta for "nenhum", não escreva.

Se a lógica está presa numa classe difícil de testar (dependência estática, dado de ambiente), **extraia a parte pura** — ex.: parser recebendo `IEnumerable<string>` — e teste a regra. Verifique antes se já existe construtor `internal` para teste (padrão do projeto, ex.: `ConfiguracaoService(string test_filePath)`).

Depois de decidir, para as classes que se qualificam: siga o padrão `NomeClasseTests.cs` existente.

### 3. Validar code style

Rode o checklist da skill `validar-code-style` nos arquivos modificados — o plugin `.opencode/plugins/validacao/` já checa as regras mecânicas e injeta as violações no prompt.

Se encontrar violação, **corrija imediatamente** antes de prosseguir.

### 4. Criar/atualizar testes (se necessário)

Se na etapa 2 foi identificada necessidade de testes:

**A)** Para cada arquivo `.cs` que passou no critério da etapa 2 e não tem teste correspondente:
  - Usar o subagent `@testador` com instruções detalhadas sobre qual **regra** testar (não apenas "teste esta classe")
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

### 9. Restaurar usuário do gh

Sempre execute ao final — mesmo que alguma etapa anterior tenha falhado antes do commit/push — o comando para devolver o gh ao usuário da deeprocket:

```bash
gh auth switch --user wallace-deeprocket
```

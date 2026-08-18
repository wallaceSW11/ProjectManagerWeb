---
name: validar-code-style
description: Valida manualmente se arquivos modificados seguem o code-style do projeto (.cs, .vue, .ts).
---

# Skill: validar-code-style

Use quando quiser verificar code-style manualmente, complementar o plugin automático, ou revisar antes de entregar.

## Backend (.cs)

- AutoMapper (`AutoMapper`, `CreateMap`, `IMapper`)
- `.Result` / `.Wait()` — deve ser async/await
- `Console.WriteLine` (exceto `Program.cs`)
- Catch vazio — sempre tratar ou relançar
- Controller lendo JSON direto — delegar ao Service
- `ActionResult<T>` — usar `IActionResult`

## Frontend (.vue / .ts)

- `.then()` — usar async/await
- `var` — usar const/let
- `console.log` — remover
- `!important` — usar classes Vuetify
- Prefixo `is`/`has` em booleano
- Lógica complexa em v-if/v-show/:class — extrair pra computed
- Loading sem finally — resetar no finally
- Chamar service direto do component — passar pela store

## Revisão qualitativa — boas práticas

- **SOLID** — responsabilidade única: sem lógica de negócio em controller/component; camadas respeitadas (Component → Store → Service → API | Controller → Service → JsonService)
- **DRY** — sem duplicação; se repetir 2x+, extrair util/composable/serviço
- **KISS** — solução mais simples possível; sem abstração/interface sem necessidade (YAGNI)
- **Não reinventar a roda** — ler arquivo similar antes de criar novo; reaproveitar padrões existentes do projeto
- Sem placeholder/TODO; sem código morto; nomes auto-documentados

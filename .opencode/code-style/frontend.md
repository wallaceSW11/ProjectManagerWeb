# Code Style — Frontend Vue 3 + TypeScript

Boilerplates completos (store, model, carregamento, errado vs certo): `.opencode/referencias/frontend-boilerplates.md`.

## Proibido

- `.then().catch()` — async/await sempre
- Lógica no template — computed/methods
- `var` — const/let
- `any` desnecessário — tipar tudo
- Chamar service direto do component — passe pela store
- Pular camadas: Component → Store → Service → API
- `!important` no CSS
- Hardcodar cores — variáveis do Vuetify
- `console.log` — snackbar/notify para erros
- Prefixo `is`/`has` em booleanos: `loading`, não `isLoading`
- `booleano ?? false` — converta com `!!` (`ativo: !!obj.usoRecursos?.ativo`). `??` fica só para valores onde `0`/`''` são válidos
- If de uma linha COM chaves `{}` — early return incluso.
  ✅ `if (!item) return;`
  ❌ `if (!item) { return; }`
- If/else — ternário pra 2 caminhos ou objeto mapeado pra múltiplos.
- `watch`/`watchEffect` — último recurso. Prefira `computed`, `@update:model-value` e handler de evento.

## Async

Loading reseta no `finally`. Nunca no try ou catch.

## Respiração vertical

Separe parágrafos lógicos com linha em branco — nunca cole `const`, early return e `return` final.
Early returns consecutivos ficam juntos, sem linha em branco entre eles.

## Template

Zero lógica. Condicional e class binding → computed.

## Componentes

PascalCase: `<ListaProdutos />`.
Boolean props sem valor: `<v-btn desabilitado />`.
Max 200 linhas. Template repetido 2x+ = extrair componente.

## Nomenclatura de arquivos

```
Componente:  ListaProdutos.vue
Store:       useProdutoStore.ts
Service:     produtoService.ts
Model/Type:  Produto.ts
```

## Pinia — Options API style

`state` + `getters` + `actions`, como nas stores existentes. Lista: `String()` nos dois lados do `findIndex` + `splice`, sem ligar o loading global.

## Vuetify

`pa-*`, `ma-*`, `ga-*`, `d-flex`, `text-*`, `font-weight-*`.
CSS scoped só quando Vuetify não cobre. `variant="outlined"` nos inputs.

## Ordem no script setup

1. imports (vue → services → models → stores → components → utils)
2. defineProps / defineEmits
3. stores
4. refs
5. computed
6. functions
7. lifecycle (onMounted...)

## Models

Classe com `constructor(obj: Partial<IModel>)` + `toDTO()`. Interface em `types/index.ts`.

Booleano opcional no constructor usa `!!`: `this.ativo = !!obj.ativo;`. `?? false` só quando o default for explícito e o campo não for booleano.

## ProjetoModel — dois formatos de `comandos`

`GET /api/pastas` → `comandos` como `string[]`
`GET /api/repositorios` → `comandos` como objeto `ComandoDTO`

O model detecta e normaliza no constructor.

## Sem comentários

Nomes auto-documentados. Zero comentários no código.

## Confirmações destrutivas

`confirm()` nativo — sem modal customizado.

## DELETE com body no axios

```ts
await this.delete('pastas', { data: { diretorio } });
```

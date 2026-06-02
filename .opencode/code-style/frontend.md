# Code Style — Frontend Vue 3 + TypeScript

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

## Async

Loading reseta no `finally`. Nunca no try ou catch.

```ts
async function carregar(): Promise<void> {
  carregando.value = true;
  try {
    dados.value = await servico.obterTodos();
  } catch {
    notificar.erro('Erro', 'Erro ao carregar dados.');
  } finally {
    carregando.value = false;
  }
}
```

## Template

Zero lógica. Condicionais e class bindings → computed.

```vue
<!-- errado -->
<v-btn v-if="itens.length > 0 && !carregando">Salvar</v-btn>

<!-- certo -->
<v-btn v-if="podeSalvar">Salvar</v-btn>
const podeSalvar = computed(() => itens.value.length > 0 && !carregando.value)
```

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

Seguir o padrão das stores existentes:

```ts
export const useXStore = defineStore('x', {
  state: (): XState => ({
    itens: [],
    carregando: false,
    erro: null,
  }),

  getters: {
    total: (state): number => state.itens.length,
  },

  actions: {
    async carregar(): Promise<void> {
      this.carregando = true;
      this.erro = null;
      try {
        this.itens = await xService.obterTodos();
      } catch (erro: any) {
        this.erro = erro.message || 'Erro ao carregar';
        throw erro;
      } finally {
        this.carregando = false;
      }
    },
  },
});
```

## Atualização de item na lista

Usar `String()` nos dois lados do `findIndex`. Usar `splice`. Não ativar loading global.

```ts
const indice = itens.value.findIndex(o => String(o.id) === String(id));
if (indice !== -1) itens.value.splice(indice, 1, atualizado);
```

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

```ts
export default class RepositorioModel implements IRepositorio {
  identificador: string;
  url: string | null;

  constructor(obj: Partial<IRepositorio> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.url = obj.url || null;
  }

  paraDTO() {
    return { identificador: this.identificador, url: this.url };
  }
}
```

## ProjetoModel — dois formatos de `comandos`

`GET /api/pastas` → `comandos` como `string[]`
`GET /api/repositorios` → `comandos` como objeto `ComandoDTO`

O model detecta e normaliza no constructor.

## Sem comentários

Nomes auto-documentados. Zero comentários no código.

## Confirmações destrutivas

Usar `confirm()` nativo:

```ts
const confirmado = confirm(`Tem certeza que deseja excluir?\n\n${diretorio}\n\nEsta ação não pode ser desfeita.`);
if (!confirmado) return;
```

## DELETE com body no axios

```ts
await this.delete('pastas', { data: { diretorio } });
```

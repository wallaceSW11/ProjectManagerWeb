---
inclusion: fileMatch
fileMatchPattern: "frontend/**"
---

# rule-code-style-frontend

Regras de código para o frontend Vue 3 + TypeScript. Sem exceções.

## proibido

- NUNCA .then().catch() — sempre async/await
- NUNCA lógica no template — computed/methods apenas
- NUNCA var — sempre const/let
- NUNCA any desnecessário — tipar tudo
- NUNCA chamar service do component — passar pela store
- NUNCA pular camadas: Component → Store → Service → API
- NUNCA !important no CSS
- NUNCA hardcodar cores — variáveis do Vuetify
- NUNCA console.log no código entregue — usar alert ou snackbar para erros ao usuário
- NUNCA prefixo is/has em booleanos: `loading`, não `isLoading`

## async

Loading reseta no `finally`. Nunca no try ou catch.

```ts
async function load(): Promise<void> {
  loading.value = true;
  try {
    data.value = await service.getAll();
  } catch {
    notify.error('Erro', 'Erro ao carregar dados.');
  } finally {
    loading.value = false;
  }
}
```

## template

Zero lógica. Condicionais e class bindings → computed.

```vue
<!-- errado -->
<v-btn v-if="items.length > 0 && !loading">Salvar</v-btn>

<!-- certo -->
<v-btn v-if="canSave">Salvar</v-btn>
// const canSave = computed(() => items.value.length > 0 && !loading.value)
```

## componentes

PascalCase: `<ProductList />` nunca `<product-list />`.
Boolean props sem valor: `<v-btn disabled />`.
Max 200 linhas. Template repetido 2x+ = extrair componente.

## typescript

Tipar tudo. Sem any.

```ts
const products = ref<Product[]>([]);
async function load(slug: string): Promise<void> {}
```

## nomenclatura de arquivos

```
Componente:  ProductList.vue
Store:       useProductStore.ts
Service:     productService.ts
Composable:  useFormatCurrency.ts
Model/Type:  Product.ts
```

## camadas

```
Component → Store → Service → API
```

Service: HTTP apenas. Store: lógica + estado. Component: UI apenas.

```ts
// errado
const products = await productService.getAll(bakeryId);

// certo
await productStore.load(bakeryId);
```

## pinia — estilo das stores

As stores existentes usam **Options API style**. Ao criar nova store, seguir o mesmo padrão.

```ts
export const useXStore = defineStore('x', {
  state: (): XState => ({
    items: [],
    loading: false,
    error: null,
  }),

  getters: {
    count: (state): number => state.items.length,
  },

  actions: {
    async carregar(): Promise<void> {
      this.loading = true;
      this.error = null;
      try {
        this.items = await xService.getAll();
      } catch (error: any) {
        this.error = error.message || 'Erro ao carregar';
        throw error;
      } finally {
        this.loading = false;
      }
    },
  },
});
```

## pinia — atualização de item na lista

Ao atualizar um item retornado pela API, sempre usar `String()` em ambos os lados do `findIndex`. Entidades têm `id: number`, mas funções de store recebem `id: string` — a comparação direta retorna `-1` silenciosamente e a lista não atualiza.

Usar `splice` em vez de atribuição por índice — garante detecção de mudança pelo Proxy do Vue 3, especialmente com `v-data-table` do Vuetify.

Updates pontuais de item **não devem** ativar o `loading` global da store — isso faz a tabela entrar em estado de loading e pode mascarar a atualização visual.

```ts
// errado
loading.value = true;
const index = items.value.findIndex(o => o.id === id);
items.value[index] = updated;
loading.value = false;

// certo
const index = items.value.findIndex(o => String(o.id) === String(id));
if (index !== -1) items.value.splice(index, 1, updated);
```

## vuetify

Utilitários Vuetify: `pa-*`, `ma-*`, `ga-*`, `d-flex`, `text-*`, `font-weight-*`.
CSS scoped só quando Vuetify não cobre. Nunca `!important`.
Sempre `variant="outlined"` nos campos de input.

## ordem no script setup

```ts
// 1. imports (vue → services → models → stores → components → utils)
// 2. defineProps / defineEmits
// 3. stores
// 4. refs
// 5. computed
// 6. functions
// 7. lifecycle (onMounted...)
```

## models — padrão obrigatório

Toda entidade de domínio é uma classe com `constructor(obj: Partial<IModel>)` e `toDTO()`.
Nunca criar model como interface pura ou objeto literal — sempre classe seguindo o padrão.

```ts
// errado
const repositorio = { identificador: crypto.randomUUID(), url: null, ... }

// certo
export default class RepositorioModel implements IRepositorio {
  identificador: string;
  url: string | null;

  constructor(obj: Partial<IRepositorio> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.url = obj.url || null;
  }

  toDTO() {
    return { identificador: this.identificador, url: this.url };
  }
}
```

Interfaces ficam em `src/types/index.ts`. Classes ficam em `src/models/`.
Ao instanciar dados da API: `new RepositorioModel(response)` — nunca usar o objeto cru diretamente.

## ProjetoModel — dois formatos de entrada

O backend envia `comandos` em dois formatos diferentes dependendo do endpoint:

- `GET /api/pastas` → `comandos` chega como `string[]` (array de `ETipoComando`)
- `GET /api/repositorios` → `comandos` chega como objeto `ComandoDTO` `{ instalar, iniciar, buildar, ideIdentificador }`

O `ProjetoModel` detecta o formato no constructor e normaliza:
- Se `comandos` é objeto (não array) → popula `comandosObj` e chama `syncComandosFromObj()` para gerar o array
- Se `comandos` é array → usa diretamente, `comandosObj` fica com defaults nulos

Nunca assumir que `comandos` chegará sempre como array — depende de qual endpoint originou o dado.

## comentários

Zero comentários. Nomes auto-documentados.

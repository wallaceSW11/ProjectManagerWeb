# Boilerplates — Frontend Vue 3 + TypeScript

Referência de código real do PMW. As regras estão em `.opencode/code-style/frontend.md`.

## Store Pinia (Options API)

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

## Carregamento em componente

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

## Atualização de item na lista

```ts
const indice = itens.value.findIndex(o => String(o.id) === String(id));
if (indice !== -1) itens.value.splice(indice, 1, atualizado);
```

## Model

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

## `watch` — errado vs certo

```ts
watch(menuAberto, aberto => {
  if (!aberto) selecionados.value = [];
});

const aoAlternarMenu = (aberto: boolean): void => {
  if (aberto) return;
  selecionados.value = [];
};
```

```vue
<v-menu v-model="menuAberto" @update:model-value="aoAlternarMenu" />
```

## Template — errado vs certo

```vue
<v-btn v-if="itens.length > 0 && !carregando">Salvar</v-btn>

<v-btn v-if="podeSalvar">Salvar</v-btn>
```

```ts
const podeSalvar = computed(() => itens.value.length > 0 && !carregando.value)
```

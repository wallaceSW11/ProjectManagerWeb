<template>
  <div class="d-flex align-center gap-1">
    <v-combobox
      label="Branch"
      v-model="branch"
      :rules="regras"
      :items="itemsFiltrados"
      :disabled="disabled"
      :loading="loading"
      :hint="hint"
      :error="error"
      :persistent-hint="!!hint"
      autocomplete="off"
      hide-no-data
      class="flex-grow-1"
      @blur="emit('blur')"
    >
      <template #item="{ props, item }">
        <v-list-item
          v-bind="props"
          :title="item.title"
        >
          <template #append>
            <v-btn
              icon="mdi-close"
              size="x-small"
              variant="text"
              @click.stop="removerBranch(item.title)"
            />
          </template>
        </v-list-item>
      </template>
    </v-combobox>

    <IconeComTooltip
      icone="mdi-clipboard-outline"
      texto="Colar da área de transferência"
      :acao="colarDoClipboard"
    />
  </div>
</template>

<script setup lang="ts">
  import { computed, onMounted, ref, watch } from 'vue';

  const branch = defineModel<string>({ default: '' });
  const props = defineProps<{
    obrigatorio: any;
    adicionarNoLocalStorage?: boolean;
    disabled?: boolean;
    hint?: string;
    error?: boolean;
    loading?: boolean;
  }>();

  const emit = defineEmits<{
    blur: [];
  }>();

  const regras = computed(() => {
    return props.obrigatorio ? [(v: string) => !!v || 'Obrigatório'] : [];
  });

  const itemsFiltrados = computed(() => {
    if (!branch.value) return branchs.value;
    return branchs.value.filter(item => 
      item.toLowerCase().includes(branch.value.toLowerCase())
    );
  });

  watch(
    () => props.adicionarNoLocalStorage,
    () => {
      if (props.adicionarNoLocalStorage) {
        adicionarBranchNoLocalStorage(branch.value);
      }
    }
  );

  const branchs = ref<string[]>([]);
  const CHAVE_BRANCHS = 'branchs';

  const obterBranchsLocalStorage = () => {
    const branchsLocalStorage = localStorage.getItem(CHAVE_BRANCHS);
    if (branchsLocalStorage) {
      branchs.value = JSON.parse(branchsLocalStorage);
    }
  };

  const adicionarBranchNoLocalStorage = (branch: string) => {
    if (!branchs.value.includes(branch)) {
      branchs.value.push(branch);
      localStorage.setItem(CHAVE_BRANCHS, JSON.stringify(branchs.value));
    }
  };

  const removerBranch = (branchParaRemover: string) => {
    branchs.value = branchs.value.filter(b => b !== branchParaRemover);
    localStorage.setItem(CHAVE_BRANCHS, JSON.stringify(branchs.value));
  };

  const colarDoClipboard = async (): Promise<void> => {
    try {
      const texto = await navigator.clipboard.readText();
      if (texto) branch.value = texto.trim();
    } catch {
      // permissão negada ou clipboard vazio — silencioso
    }
  };

  onMounted(() => {
    obterBranchsLocalStorage();
  });
</script>

<template>
  <v-combobox
    label="Branch"
    v-model="branch"
    :rules="regras"
    :items="itemsFiltrados"
    autocomplete="off"
    hide-no-data
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
</template>

<script setup lang="ts">
  import { computed, onMounted, ref, watch } from 'vue';

  const branch = defineModel<string>({ default: '' });
  const props = defineProps<{
    obrigatorio: any;
    adicionarNoLocalStorage?: boolean;
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

  const obterBranchsLocalStorage = () => {
    const branchsLocalStorage = localStorage.getItem('branchs');
    if (branchsLocalStorage) {
      branchs.value = JSON.parse(branchsLocalStorage);
    }
  };

  const adicionarBranchNoLocalStorage = (branch: string) => {
    if (!branchs.value.includes(branch)) {
      branchs.value.push(branch);
      localStorage.setItem('branchs', JSON.stringify(branchs.value));
    }
  };

  const removerBranch = (branchParaRemover: string) => {
    branchs.value = branchs.value.filter(b => b !== branchParaRemover);
    localStorage.setItem('branchs', JSON.stringify(branchs.value));
  };

  onMounted(() => {
    obterBranchsLocalStorage();
  });
</script>

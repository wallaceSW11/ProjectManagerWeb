<template>
  <v-select
    label="Repositório"
    :items="repositorios"
    item-title="titulo"
    v-model="repositorio"
    :rules="regras"
    :loading="carregando"
    return-object
  >
    <template #item="{ item, props }">
      <v-list-item
        v-bind="props"
        class="pb-2"
      >
        <v-list-item-subtitle>{{ item.raw.url }}</v-list-item-subtitle>
      </v-list-item>
    </template>
  </v-select>
</template>

<script setup lang="ts">
  import { computed, onMounted, ref } from 'vue';
  import type { IRepositorio } from '@/types';
  import RepositoriosService from '@/services/RepositoriosService';

  interface Props {
    obrigatorio?: boolean;
  }

  const props = withDefaults(defineProps<Props>(), {
    obrigatorio: false,
  });

  const repositorios = ref<IRepositorio[]>([]);
  const repositorio = defineModel<IRepositorio>();
  const carregando = ref<boolean>(false);

  const regras = computed(() => {
    return props.obrigatorio
      ? [(v: IRepositorio) => (!!v?.titulo && !!v?.url) || 'Obrigatório']
      : [];
  });

  onMounted(async () => {
    try {
      carregando.value = true;
      repositorios.value = await RepositoriosService.getRepositorios();
    } catch (error) {
      console.error('Falha ao obter os repositórios', error);
    } finally {
      carregando.value = false;
    }
  });
</script>

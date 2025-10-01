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
      <v-list-item v-bind="props" class="pb-2">
        <v-list-item-subtitle>{{ item.raw.url }}</v-list-item-subtitle>
      </v-list-item>
    </template>
  </v-select>
</template>

<script setup>
const repositorios = ref([]);
import RepositorioModel from "@/models/RepositorioModel";
import RepositoriosService from "@/services/RepositoriosService";
import { computed, onMounted, ref } from "vue";

const props = defineProps({
  obrigatorio: {
    type: Boolean,
    default: false,
  },
});

const regras = computed(() => {
  return props.obrigatorio ? [(v) => (!!v?.titulo && !!v?.url) || "Obrigatório"] : [];
});

const repositorio = defineModel();
const carregando = ref(false)

onMounted(async () => {
  try {
    carregando.value = true
    repositorios.value = await RepositoriosService.getRepositorios();
  } catch (error) {
    console.error("Falha ao obter os repositórios", error);
  } finally {
    carregando.value = false
  }
});
</script>

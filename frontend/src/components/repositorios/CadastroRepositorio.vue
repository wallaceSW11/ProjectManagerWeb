<template>
  <v-form ref="formRepositorio">
    <v-row no-gutters>
      <v-col cols="12">
        <v-text-field
          label="Nome"
          v-model="repositorio.nome"
          :rules="obrigatorio"
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Url"
          v-model="repositorio.url"
          :rules="obrigatorio"
        />
      </v-col>

      <v-col cols="12">
        <v-select
          label="Agregados"
          v-model="repositorio.agregados"
          :items="repositoriosDisponiveis"
          item-title="nome"
          item-value="id"
          multiple
          chips
          clearable
        />
      </v-col>
    </v-row>
  </v-form>
</template>

<script setup>
import { computed } from "vue";
import RepositorioModel from "../../models/RepositorioModel";

const props = defineProps({
  repositorios: {
    type: Array,
    required: true,
  },
});

const repositorio = defineModel(new RepositorioModel());

const obrigatorio = [(v) => !!v || "Obrigatório"];


const repositoriosDisponiveis = computed(() => {
  return props.repositorios.filter((r) => r.id !== repositorio.value.id);
});
</script>
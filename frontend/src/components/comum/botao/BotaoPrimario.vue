<template>
  <v-btn
    color="primary"
    variant="outlined"
    @click="acao"
    :disabled="desabilitar"
  >
    <v-icon v-if="icone" class="pr-2">{{ icone }}</v-icon>
    {{ texto }}
  </v-btn>
</template>

<script setup>
const props = defineProps({
  texto: {
    type: String,
    default: "Salvar",
  },
  icone: {
    type: String,
    default: null,
  },
  desabilitar: {
    type: Boolean,
    default: false,
  },
  acao: {
    type: Function,
    default: null,
  },
});
</script>

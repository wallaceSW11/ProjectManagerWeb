<template>
  <v-dialog v-model="exibirModal" max-width="600px">
    <v-card>
      <v-card-title class="text-h5">{{ titulo }}</v-card-title>
      <v-card-text>
        <slot />
      </v-card-text>
      <v-card-actions>
        <v-spacer></v-spacer>
        <v-btn text @click="acaoBotaoPrimario"> {{ textoBotaoPrimario }}</v-btn>
        <v-btn color="primary" @click="acaoBotaoSecundario"> {{ textoBotaoSecundario }}</v-btn>
      </v-card-actions>

    </v-card>
  </v-dialog>
</template>

<script setup>
const exibirModal = defineModel(false);

const props = defineProps({
  titulo: {
    type: String,
    required: true,
  },
  textoBotaoPrimario: {
    type: String,
    default: 'Salvar',
  },
  textoBotaoSecundario: {
    type: String,
    default: 'Cancelar',
  },
  exibirBotaoPrimario: {
    type: Boolean,
    default: true,
  },
  exibirBotaoSecundario: {
    type: Boolean,
    default: true,
  },
  acaoBotaoPrimario: {
    type: Function,
    default: null,
  },
  acaoBotaoSecundario: {
    type: Function,
    default: null,
  },
});
</script>
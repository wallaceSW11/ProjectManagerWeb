<template>
  <v-dialog
    v-model="exibirModal"
    :min-width="larguraMinima"
    :persistent="persistent"
  >
    <v-card>
      <v-card-title class="text-h5">{{ titulo }}</v-card-title>
      <v-card-text>
        <slot />
      </v-card-text>
      <v-card-actions class="pr-4 pb-4">
        <v-spacer></v-spacer>
        <BotaoPrimario
          @click="acaoBotaoPrimario"
          :text="textoBotaoPrimario"
          :desabilitar="desabilitarBotaoPrimario"
        />
        <BotaoSecundario
          @click="acaoBotaoSecundario"
          :text="textoBotaoSecundario"
          :desabilitar="desabilitarBotaoSecundario"
        />
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
    default: "Salvar",
  },
  textoBotaoSecundario: {
    type: String,
    default: "Cancelar",
  },
  exibirBotaoPrimario: {
    type: Boolean,
    default: true,
  },
  exibirBotaoSecundario: {
    type: Boolean,
    default: true,
  },
  desabilitarBotaoPrimario: {
    type: Boolean,
    default: false,
  },
  desabilitarBotaoSecundario: {
    type: Boolean,
    default: false,
  },
  acaoBotaoPrimario: {
    type: Function,
    default: null,
  },
  acaoBotaoSecundario: {
    type: Function,
    default: null,
  },
  larguraMinima: {
    type: String,
    default: "600px",
  },
  persistent: {
    type: Boolean,
    default: true,
  },
});
</script>

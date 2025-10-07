<template>
  <v-dialog
    v-model="exibirModal"
    :min-width="larguraMinima"
    :max-width="larguraMaxima"
    :persistent="persistent"
    scrollable
  >
    <v-card>
      <v-card-title class="text-h5">{{ titulo }}</v-card-title>
      <v-card-text>
        <slot />
      </v-card-text>
      <v-card-actions class="pr-4 pb-4">
        <v-spacer></v-spacer>
        <BotaoPrimario
          v-if="exibirBotaoPrimario"
          @click="acaoBotaoPrimario"
          :texto="textoBotaoPrimario"
          :desabilitar="desabilitarBotaoPrimario"
          :icone="iconeBotaoPrimario"
        />
        <BotaoSecundario
          v-if="exibirBotaoSecundario"
          @click="acaoBotaoSecundario"
          :texto="textoBotaoSecundario"
          :desabilitar="desabilitarBotaoSecundario"
          :icone="iconeBotaoSecundario"
        />
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
interface Props {
  titulo: string
  textoBotaoPrimario?: string
  textoBotaoSecundario?: string
  exibirBotaoPrimario?: boolean
  exibirBotaoSecundario?: boolean
  desabilitarBotaoPrimario?: boolean
  desabilitarBotaoSecundario?: boolean
  acaoBotaoPrimario?: (() => void) | null
  acaoBotaoSecundario?: (() => void) | null
  larguraMinima?: string
  larguraMaxima?: string
  persistent?: boolean
  iconeBotaoPrimario?: string
  iconeBotaoSecundario?: string
}

const exibirModal = defineModel<boolean>({ default: false })

withDefaults(defineProps<Props>(), {
  textoBotaoPrimario: 'Salvar',
  textoBotaoSecundario: 'Cancelar',
  exibirBotaoPrimario: true,
  exibirBotaoSecundario: true,
  desabilitarBotaoPrimario: false,
  desabilitarBotaoSecundario: false,
  acaoBotaoPrimario: null,
  acaoBotaoSecundario: null,
  larguraMinima: '600px',
  larguraMaxima: '800px',
  persistent: true,
  iconeBotaoPrimario: 'mdi-plus',
  iconeBotaoSecundario: 'mdi-cancel',
})
</script>

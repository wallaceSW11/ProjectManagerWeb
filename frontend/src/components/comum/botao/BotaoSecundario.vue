<template>
  <v-btn
    variant="plain"
    @click="acao"
    :disabled="desabilitar"
  >
    <v-icon v-if="icone" class="pr-2">{{ icone }}</v-icon>
    {{ texto }}
  </v-btn>
</template>

<script setup lang="ts">
interface Props {
  texto?: string
  icone?: string | null
  desabilitar?: boolean
  acao?: (() => void) | null
}

withDefaults(defineProps<Props>(), {
  texto: 'Salvar',
  icone: null,
  desabilitar: false,
  acao: null,
})
</script>

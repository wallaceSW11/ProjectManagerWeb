<template>
  <v-tooltip
    :text="texto"
    :location="getLocation()"
  >
    <template #activator="{ props }">
      <v-icon
        v-if="semBotao"
        small
        :color="cor"
        v-bind="props"
        >{{ icone }}</v-icon
      >
      <v-btn
        v-else
        v-bind="props"
        icon
        size="small"
        variant="flat"
        @click="acao()"
        :disabled="desabilitado"
      >
        <v-icon
          small
          :color="cor"
          >{{ icone }}</v-icon
        >
      </v-btn>
    </template>
  </v-tooltip>
</template>

<script setup lang="ts">
  type LocationType = 'top' | 'bottom' | 'start' | 'end'

  interface Props {
    icone: string
    texto: string
    acao?: () => void
    top?: boolean
    bottom?: boolean
    left?: boolean
    right?: boolean
    desabilitado?: boolean
    cor?: string
    semBotao?: boolean
  }

  const props = withDefaults(defineProps<Props>(), {
    acao: () => {},
    top: true,
    bottom: false,
    left: false,
    right: false,
    desabilitado: false,
    cor: undefined,
    semBotao: false,
  })

  const getLocation = (): LocationType => {
    if (props.top) return 'top'
    if (props.bottom) return 'bottom'
    if (props.left) return 'start'
    if (props.right) return 'end'
    return 'top'
  }
</script>

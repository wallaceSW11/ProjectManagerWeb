<template>
  <div class="d-inline-flex ga-1">
    <IconeComTooltip
      icone="mdi-chevron-up"
      texto="Subir"
      :acao="subir"
      :desabilitado="primeiro"
      top
    />
    <IconeComTooltip
      icone="mdi-chevron-down"
      texto="Descer"
      :acao="descer"
      :desabilitado="ultimo"
      top
    />
  </div>
</template>

<script setup lang="ts">
  import IconeComTooltip from './IconeComTooltip.vue';

  interface Props {
    primeiro: boolean;
    ultimo: boolean;
  }

  defineProps<Props>();

  const emit = defineEmits<{
    subir: [];
    descer: [];
  }>();

  const subir = (): void => emit('subir');
  const descer = (): void => emit('descer');
</script>

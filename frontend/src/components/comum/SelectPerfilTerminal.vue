<template>
  <v-select
    :label="label"
    v-model="perfil"
    :items="perfisTerminal"
    :loading="loading"
    clearable
    :hint="hint"
    persistent-hint
  />
</template>

<script setup lang="ts">
  import { onMounted, ref } from 'vue';
  import TerminalService from '@/services/TerminalService';

  const perfil = defineModel<string | null>({ default: null });

  withDefaults(defineProps<{
    label?: string;
    hint?: string;
  }>(), {
    label: 'Perfil do Terminal',
    hint: 'Perfil do Windows Terminal'
  });

  const perfisTerminal = ref<string[]>([]);
  const loading = ref(false);

  onMounted(async () => {
    loading.value = true;
    try {
      perfisTerminal.value = await TerminalService.getPerfis();
    } finally {
      loading.value = false;
    }
  });
</script>

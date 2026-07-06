<template>
  <v-select
    label="Perfil da IDE"
    v-model="perfil"
    :items="perfis"
    :loading="carregando"
    :disabled="!ideNome"
    clearable
    hint="Perfil usado ao abrir o projeto na IDE"
    persistent-hint
  />
</template>

<script setup lang="ts">
  import { computed, ref, watch } from 'vue';
  import ConfiguracaoService from '@/services/ConfiguracaoService';

  const perfil = defineModel<string | null>({ default: null });

  const props = withDefaults(
    defineProps<{
      ideIdentificador?: string | null;
      ides: { identificador: string; nome: string }[];
    }>(),
    { ideIdentificador: null }
  );

  const perfis = ref<string[]>([]);
  const carregando = ref(false);

  const ideNome = computed((): string | null => {
    if (!props.ideIdentificador) return null;
    const ide = props.ides.find(
      i => i.identificador === props.ideIdentificador
    );
    return ide?.nome || null;
  });

  watch(ideNome, async novoNome => {
    if (!novoNome) {
      perfis.value = [];
      perfil.value = null;
      return;
    }

    carregando.value = true;
    try {
      perfis.value = await ConfiguracaoService.obterPerfisIDE(novoNome);
      if (perfis.value.length === 0) perfil.value = null;
    } catch {
      perfis.value = [];
    } finally {
      carregando.value = false;
    }
  });
</script>

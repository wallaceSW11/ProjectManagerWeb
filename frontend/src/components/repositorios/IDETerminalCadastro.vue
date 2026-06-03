<template>
  <v-row no-gutters>
    <v-col
      cols="12"
      class="mb-4"
    >
      <v-select
        label="IDE para abrir"
        v-model="repositorio.ideIdentificador"
        :items="ides"
        item-title="nome"
        item-value="identificador"
        clearable
        hint="IDE padrão para abrir a pasta do repositório"
        persistent-hint
      />
    </v-col>

    <v-col
      cols="12"
      class="mb-4"
    >
      <v-checkbox
        v-model="repositorio.abrirWorkspace"
        label="Abrir workspace"
        hint="Se marcado, abre o arquivo .code-workspace quando existir no diretório"
        persistent-hint
        hide-details="auto"
      />
    </v-col>

    <v-col
      cols="12"
      class="mb-4"
    >
      <v-select
        :items="configuracaoStore.perfisVSCode"
        label="Perfil da IDE"
        v-model="repositorio.perfilVSCode"
        item-title="nome"
        item-value="nome"
        clearable
        hint="Perfil usado ao abrir a pasta raiz do repositório na IDE"
        persistent-hint
      />
    </v-col>

    <v-col
      cols="12"
      class="mb-4"
    >
      <v-select
        label="CLI de IA"
        v-model="repositorio.cliComando"
        :items="configuracaoStore.clis"
        item-title="nome"
        item-value="comando"
        clearable
        hint="CLI usada para abrir o repositório (ex: Kiro CLI, Claude Code)"
        persistent-hint
      />
    </v-col>

    <v-col
      cols="12"
      class="mb-4"
    >
      <v-text-field
        label="Comando complementar da CLI"
        v-model="repositorio.cliComandoComplementar"
        hint='Concatenado ao comando da CLI (ex: chat --agent "delphi-dev")'
        persistent-hint
        clearable
      />
    </v-col>

    <v-col
      cols="12"
      class="mb-4"
    >
      <SelectPerfilTerminal
        v-model="repositorio.perfilTerminal"
        hint="Perfil do Windows Terminal usado ao abrir a CLI de IA"
      />
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
  import { onMounted, ref } from 'vue';
  import type { IRepositorio, IIDE } from '@/types';
  import IDEsService from '@/services/IDEsService';
  import { useConfiguracaoStore } from '@/stores/configuracao';
  import SelectPerfilTerminal from '@/components/comum/SelectPerfilTerminal.vue';

  const repositorio = defineModel<IRepositorio>({ required: true });
  const configuracaoStore = useConfiguracaoStore();

  const ides = ref<IIDE[]>([]);

  onMounted(async () => {
    await carregarIDEs();
  });

  const carregarIDEs = async (): Promise<void> => {
    try {
      const resposta = await IDEsService.getIDEs();
      ides.value = resposta;

      if (!repositorio.value.ideIdentificador && ides.value.length > 0)
        repositorio.value.ideIdentificador = ides.value[0].identificador;
    } catch (error) {
      console.error('Falha ao carregar IDEs:', error);
    }
  };
</script>

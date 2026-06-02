<template>
  <v-form ref="formRepositorio">
    <v-row no-gutters>
      <v-col cols="12">
        <v-text-field
          label="Url"
          v-model="repositorio.url"
          :rules="obrigatorio"
          @change="atualizarNomeRepositorio()"
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Nome"
          v-model="repositorio.nome"
          :rules="obrigatorio"
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Título"
          v-model="repositorio.titulo"
          :rules="obrigatorio"
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Branch base"
          v-model="repositorio.branchBase"
          hint="Branch principal do repositório (develop, dev, main)"
          persistent-hint
          placeholder="develop"
          clearable
        />
      </v-col>

      <v-col cols="12">
        <v-select
          label="Pasta centralizadora"
          v-model="repositorio.pastaCentralizadora"
          :items="pastasCentralizadoras"
          clearable
          hint="Pasta onde os clones serão criados"
          persistent-hint
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Cor"
          v-model="repositorio.cor"
          type="color"
          hint="Cor da borda esquerda nos cards de pasta"
          persistent-hint
        />
      </v-col>

      <v-col cols="12">
        <v-select
          label="Agregados"
          v-model="repositorio.agregados"
          :items="repositoriosDisponiveis"
          item-title="nome"
          item-value="identificador"
          multiple
          chips
          clearable
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Subdiretório"
          v-model="repositorio.subdiretorio"
          hint="Subpasta de trabalho para monorepos (ex: ProjectManagerWeb). Deixe vazio para abrir na raiz."
          persistent-hint
          clearable
        />
      </v-col>
    </v-row>
  </v-form>
</template>

<script setup lang="ts">
  import { computed, onMounted, ref } from 'vue';
  import type { IRepositorio } from '@/types';
  import { useConfiguracaoStore } from '@/stores/configuracao';

  interface Props {
    repositorios: IRepositorio[];
  }

  const props = defineProps<Props>();
  const repositorio = defineModel<IRepositorio>({ required: true });

  const configuracaoStore = useConfiguracaoStore();

  const obrigatorio = [(v: string) => !!v || 'Obrigatório'];

  const pastasCentralizadoras = computed(() => {
    return configuracaoStore.pastasCentralizadoras?.map(p => p.nome) || [];
  });

  onMounted(() => {
    if (!configuracaoStore.configuracao.pastasCentralizadoras?.length) {
      configuracaoStore.carregarConfiguracao();
    }
  });

  const repositoriosDisponiveis = computed(() => {
    return props.repositorios.filter(
      r => r.identificador !== repositorio.value.identificador
    );
  });

  const atualizarNomeRepositorio = (): void => {
    if (!repositorio.value.url) {
      repositorio.value.nome = '';
      return;
    }

    const partesUrl = repositorio.value.url.split('/');
    let nomeExtraido = partesUrl.pop() || '';

    if (nomeExtraido.endsWith('.git'))
      nomeExtraido = nomeExtraido.slice(0, -4);

    repositorio.value.nome = nomeExtraido;
  };
</script>

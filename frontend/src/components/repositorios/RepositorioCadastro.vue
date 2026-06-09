<template>
  <v-form
    ref="formRepositorio"
    autocomplete="off"
  >
    <v-row no-gutters>
      <v-col cols="12">
        <v-text-field
          ref="campoUrl"
          label="Url"
          v-model="repositorio.url"
          :rules="obrigatorio"
          autocomplete="off"
          name="pmw-url"
          @change="atualizarNomeRepositorio()"
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Nome"
          v-model="repositorio.nome"
          :rules="obrigatorio"
          autocomplete="off"
          name="pmw-nome"
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Título"
          v-model="repositorio.titulo"
          :rules="obrigatorio"
          autocomplete="off"
          name="pmw-titulo"
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
          autocomplete="off"
          name="pmw-branch"
        />
      </v-col>

      <v-col
        cols="12"
        class="pb-3"
      >
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
          label="Caminho da chave SSH"
          v-model="repositorio.caminhoChaveSSH"
          hint="Usado para autenticação SSH. Ex: /home/user/.ssh/id_ed25519. Deixe vazio para usar o SSH agent padrão."
          persistent-hint
          clearable
          autocomplete="off"
          name="pmw-ssh-key"
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Token GitHub"
          v-model="repositorio.githubToken"
          type="password"
          hint="Usado pelo gh CLI nos comandos deste repositório. Deixe vazio para usar a auth padrão."
          persistent-hint
          clearable
          autocomplete="off"
          name="pmw-github-token"
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
          autocomplete="off"
          name="pmw-subdir"
        />
      </v-col>
    </v-row>
  </v-form>
</template>

<script setup lang="ts">
  import { computed, nextTick, onMounted, ref } from 'vue';
  import type { IRepositorio } from '@/types';
  import { useConfiguracaoStore } from '@/stores/configuracao';

  interface Props {
    repositorios: IRepositorio[];
  }

  const props = defineProps<Props>();
  const repositorio = defineModel<IRepositorio>({ required: true });

  const configuracaoStore = useConfiguracaoStore();

  const obrigatorio = [(v: string) => !!v || 'Obrigatório'];
  const campoUrl = ref<InstanceType<
    typeof import('vuetify/components').VTextField
  > | null>(null);
  const formRepositorio = ref<InstanceType<
    typeof import('vuetify/components').VForm
  > | null>(null);

  function focarUrl(): void {
    nextTick(() => campoUrl.value?.focus());
  }

  defineExpose({ focarUrl, formRepositorio });

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

    if (nomeExtraido.endsWith('.git')) nomeExtraido = nomeExtraido.slice(0, -4);

    repositorio.value.nome = nomeExtraido;
  };
</script>

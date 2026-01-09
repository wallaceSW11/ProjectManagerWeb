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
    </v-row>
  </v-form>
</template>

<script setup lang="ts">
  import { computed, onMounted, ref } from 'vue';
  import type { IRepositorio, IIDE } from '@/types';
  import IDEsService from '@/services/IDEsService';

  interface Props {
    repositorios: IRepositorio[];
  }

  const props = defineProps<Props>();
  const repositorio = defineModel<IRepositorio>({ required: true });

  const obrigatorio = [(v: string) => !!v || 'Obrigatório'];
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

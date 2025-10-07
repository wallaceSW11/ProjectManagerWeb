<template>
  <v-dialog
    v-model="exibirModalPasta"
    max-width="500"
  >
    <v-card>
      <v-card-title>Cadastrar pasta</v-card-title>

      <v-card-text class="pt-0">
        <v-form ref="formPasta">
          <v-text-field
            label="Diretório"
            v-model="pasta.diretorio"
            :rules="obrigatorio"
            disabled
          />

          <SelectRepositorio
            v-model="repositorio"
            obrigatorio
          />

          <v-text-field
            label="Branch"
            v-model="pasta.branch"
            :rules="obrigatorio"
          />

          <v-text-field
            label="Número da tarefa"
            v-model.uppercase="pasta.codigo"
            class="uppercase-input"
            :rules="obrigatorio"
          />
          <v-text-field
            label="Descrição"
            v-model="pasta.descricao"
            :rules="obrigatorio"
          />

          <v-radio-group
            label="Tipo"
            inline
            hide-details
            v-model="pasta.tipo"
          >
            <v-radio
              label="Nenhum"
              value="nenhum"
            />
            <v-radio
              label="Feature"
              value="feature"
            />
            <v-radio
              label="Bug"
              value="bug"
            />
            <v-radio
              label="HotFix"
              value="hotfix"
            />
          </v-radio-group>
        </v-form>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <BotaoPrimario
          @click="criar"
          texto="Criar"
        />
        <BotaoSecundario
          @click="fecharPasta"
          texto="Fechar"
        />
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import { reactive, ref, watch } from 'vue';
  import type { IPasta, IRepositorio } from '@/types';
  import PastaModel from '@/models/PastaModel';
  import PastaService from '@/services/PastasService';
  import { useConfiguracaoStore } from '@/stores/configuracao';
  import RepositorioModel from '@/models/RepositorioModel';
  import { notificar, atualizarListaPastas } from '@/utils/eventBus';
  import SelectRepositorio from '@/components/repositorios/SelectRepositorio.vue';

  interface Props {
    pasta?: IPasta;
  }

  const props = withDefaults(defineProps<Props>(), {
    pasta: () => new PastaModel(),
  });

  const pasta = reactive<IPasta>(new PastaModel());
  const configuracaoStore = useConfiguracaoStore();
  const exibirModalPasta = defineModel<boolean>({ default: false });
  const repositorio = reactive<IRepositorio>(new RepositorioModel());
  const formPasta = ref<any>(null);

  const obrigatorio = [(v: string) => !!v || 'Obrigatório'];

  watch(exibirModalPasta, async (novoValor: boolean) => {
    if (!novoValor) return;

    pasta.diretorio = props.pasta.diretorio;

    const { codigo, descricao } = obterCodigoDescricao();
    pasta.codigo = codigo;
    pasta.descricao = descricao;
  });

  const obterCodigoDescricao = (): { codigo: string; descricao: string } => {
    const diretorio = pasta.diretorio.replace(
      configuracaoStore.diretorioRaiz + '\\',
      ''
    );

    if (!diretorio) return { codigo: '', descricao: '' };

    const comUnderscore = /^([A-Z0-9-]+)_(.+)$/i;
    const match = diretorio.match(comUnderscore);

    if (match) {
      return {
        codigo: match[1].toUpperCase(),
        descricao: match[2].replace(/_/g, ' '),
      };
    }

    const soCodigo = /^([A-Z]+\d*-?\d*)(.*)$/i;
    const matchCodigo = diretorio.match(soCodigo);

    if (matchCodigo && matchCodigo[2]) {
      return {
        codigo: matchCodigo[1].toUpperCase(),
        descricao: matchCodigo[2],
      };
    }

    return {
      codigo: '',
      descricao: diretorio,
    };
  };

  const formularioValido = async (): Promise<boolean> => {
    const form = await formPasta.value.validate();
    return form.valid;
  };

  const limparCampos = (): void => {
    Object.assign(pasta, new PastaModel());
  };

  const criar = async (): Promise<void> => {
    if (!(await formularioValido())) return;

    try {
      pasta.repositorioId = repositorio.identificador;
      await PastaService.criar(pasta);
      exibirModalPasta.value = false;
      Object.assign(pasta, new PastaModel());
      notificar('sucesso', 'Pasta cadastrada');
      atualizarListaPastas();
      limparCampos();
    } catch (error) {
      console.error('Falha ao criar pasta:', error);
      notificar('erro', 'Falha ao criar pasta');
    }
  };

  const fecharPasta = (): void => {
    exibirModalPasta.value = false;
    limparCampos();
  };
</script>

<style scoped>
  .uppercase-input :deep(input) {
    text-transform: uppercase;
  }
</style>

<template>
  <v-dialog
    v-model="exibirModalClone"
    max-width="500"
  >
    <v-card>
      <v-card-title>Clonar</v-card-title>

      <v-card-text class="pt-0">
        <v-form ref="formClone">
          <v-text-field
            label="Diretório"
            v-model="clone.diretorioRaiz"
            :rules="obrigatorio"
          />

          <SelectRepositorio
            v-model="clone.repositorio"
            obrigatorio
            carregar-ultimo-selecionado
            :chave-local-storage="CHAVE_ULTIMO_REPOSITORIO"
          />

          <SelectBranch
            v-model="clone.branch"
            obrigatorio
            :adicionarNoLocalStorage="adicionarNoLocalStorage"
            :disabled="!repositorioSelecionado"
            :loading="validandoBranch"
            :hint="hintBranch"
            :error="branchInvalida"
            persistent-hint
            @blur="validarBranch"
          />

          <v-checkbox
            label="Salvar branch no storage"
            hide-details
            v-model="clone.salvarNoStorage"
          />

          <v-checkbox
            label="Clonar agregados"
            hide-details
            v-model="clone.baixarAgregados"
            :disabled="!clone.repositorio.agregados?.length"
          />
          <v-checkbox
            label="Criar branch remoto"
            hide-details
            v-model="clone.criarBranchRemoto"
          />
          <v-checkbox
            label="Baixar histórico completo"
            hide-details
            v-model="clone.historicoCompleto"
            hint="Sem --depth, baixa o histórico completo do git"
          />

          <v-radio-group
            label="Tipo"
            inline
            hide-details
            v-model="clone.tipo"
            :disabled="!clone.criarBranchRemoto"
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
              value="bugfix"
            />
            <v-radio
              label="HotFix"
              value="hotfix"
            />
          </v-radio-group>

          <v-text-field
            label="Número da tarefa"
            v-model.uppercase="clone.codigo"
            class="uppercase-input"
            :rules="obrigatorio"
          />
          <v-text-field
            label="Descrição"
            v-model="clone.descricao"
            :rules="obrigatorio"
          />
        </v-form>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn
          color="primary"
          variant="outlined"
          :disabled="branchInvalida"
          @click="clonar"
        >
          <v-icon>mdi-download</v-icon>
          Clonar
        </v-btn>
        <v-btn @click="fecharClone">
          <v-icon>mdi-close</v-icon>
          Fechar
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue';
  import type { IClone } from '@/types';
  import CloneModel from '@/models/CloneModel';
  import CloneService from '@/services/CloneService';
  import { useConfiguracaoStore } from '@/stores/configuracao';
  import {
    notificar,
  } from '@/utils/eventBus';
  import SelectRepositorio from '@/components/repositorios/SelectRepositorio.vue';
  import SelectBranch from '@/components/comum/SelectBranch.vue';

  const CHAVE_ULTIMO_REPOSITORIO = 'pmw_ultimo_repositorio_selecionado';
  const BRANCHES_BASE = ['develop', 'dev', 'main', 'master'];

  const clone = reactive<IClone>(new CloneModel());
  const configuracaoStore = useConfiguracaoStore();
  const exibirModalClone = defineModel<boolean>({ default: false });
  const formClone = ref<any>(null);
  const adicionarNoLocalStorage = ref<boolean>(false);
  const branchInvalida = ref<boolean>(false);
  const hintBranch = ref<string>('');
  const validandoBranch = ref<boolean>(false);

  const obrigatorio = [(v: string) => !!v || 'Obrigatório'];

  const repositorioSelecionado = computed(() => !!clone.repositorio?.url);

  watch(() => clone.branch, (novaBranch) => {
    branchInvalida.value = false;
    hintBranch.value = '';
    if (!clone.criarBranchRemoto) clone.codigo = novaBranch;
  });

  watch(() => clone.criarBranchRemoto, (criarRemoto) => {
    clone.codigo = criarRemoto ? '' : clone.branch;
  });

  watch(repositorioSelecionado, (temRepositorio) => {
    if (!temRepositorio) {
      clone.branch = '';
      branchInvalida.value = false;
      hintBranch.value = '';
    }
  });

  onMounted(() => {
    clone.diretorioRaiz = configuracaoStore.diretorioRaiz + '\\';
  });

  const ehBranchBase = (branch: string): boolean =>
    BRANCHES_BASE.includes(branch.toLowerCase());

  const validarBranch = async (): Promise<void> => {
    if (!clone.branch || !repositorioSelecionado.value) return;
    if (ehBranchBase(clone.branch)) {
      branchInvalida.value = false;
      hintBranch.value = '';
      return;
    }

    validandoBranch.value = true;
    try {
      const url = clone.repositorio.url ?? '';
      const existe = await CloneService.verificarBranch(url, clone.branch);
      branchInvalida.value = !existe;
      hintBranch.value = existe ? '' : 'Branch não encontrada no remote';
    } catch {
      branchInvalida.value = false;
      hintBranch.value = '';
    } finally {
      validandoBranch.value = false;
    }
  };

  const formularioValido = async (): Promise<boolean> => {
    const form = await formClone.value.validate();
    return form.valid;
  };

  const clonar = async (): Promise<void> => {
    if (!(await formularioValido())) return;

    const payload = {
      ...clone,
      repositorioId: clone.repositorio.identificador,
    };
    payload.codigo = clone.codigo.toUpperCase();

    salvarUltimoRepositorio();

    if (clone.salvarNoStorage) {
      adicionarNoLocalStorage.value = true;
      await nextTick(() => (adicionarNoLocalStorage.value = false));
    }

    fecharClone();
    notificar('sucesso', 'Clone iniciado');

    CloneService.clonar(payload).catch(() => {
      notificar('erro', 'Falha ao clonar');
    });
  };

  const salvarUltimoRepositorio = (): void => {
    if (!clone.repositorio?.identificador) return;

    localStorage.setItem(
      CHAVE_ULTIMO_REPOSITORIO,
      clone.repositorio.identificador
    );
  };

  const fecharClone = (): void => {
    exibirModalClone.value = false;
    Object.assign(clone, new CloneModel());
    clone.diretorioRaiz = configuracaoStore.diretorioRaiz + '\\';
    branchInvalida.value = false;
    hintBranch.value = '';
  };
</script>

<style scoped>
  .uppercase-input :deep(input) {
    text-transform: uppercase;
  }
</style>

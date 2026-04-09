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
              value="bug"
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
  import { nextTick, onMounted, reactive, ref, watch } from 'vue';
  import type { IClone } from '@/types';
  import CloneModel from '@/models/CloneModel';
  import CloneService from '@/services/CloneService';
  import { useConfiguracaoStore } from '@/stores/configuracao';
  import {
    atualizarListaPastas,
    carregando,
    notificar,
  } from '@/utils/eventBus';
  import SelectRepositorio from '@/components/repositorios/SelectRepositorio.vue';
  import SelectBranch from '@/components/comum/SelectBranch.vue';

  const CHAVE_ULTIMO_REPOSITORIO = 'pmw_ultimo_repositorio_selecionado';

  const clone = reactive<IClone>(new CloneModel());
  const configuracaoStore = useConfiguracaoStore();
  const exibirModalClone = defineModel<boolean>({ default: false });
  const formClone = ref<any>(null);
  const adicionarNoLocalStorage = ref<boolean>(false);

  const obrigatorio = [(v: string) => !!v || 'Obrigatório'];

  watch(() => clone.branch, (novaBranch) => {
    if (!clone.criarBranchRemoto) clone.codigo = novaBranch;
  });

  watch(() => clone.criarBranchRemoto, (criarRemoto) => {
    clone.codigo = criarRemoto ? '' : clone.branch;
  });

  onMounted(() => {
    clone.diretorioRaiz = configuracaoStore.diretorioRaiz + '\\';
  });

  const formularioValido = async (): Promise<boolean> => {
    const form = await formClone.value.validate();
    return form.valid;
  };

  const clonar = async (): Promise<void> => {
    if (!(await formularioValido())) return;

    try {
      const payload = {
        ...clone,
        repositorioId: clone.repositorio.identificador,
      };

      payload.codigo = clone.codigo.toUpperCase();
      await CloneService.clonar(payload);

      salvarUltimoRepositorio();

      if (clone.salvarNoStorage) {
        adicionarNoLocalStorage.value = true;
        await nextTick(() => (adicionarNoLocalStorage.value = false));
      }
      fecharClone();
      Object.assign(clone, new CloneModel());
      clone.diretorioRaiz = configuracaoStore.diretorioRaiz + '\\';
      notificar('sucesso', 'Clone iniciado');
      
      carregando(true, 'Clonando...');
      setTimeout(() => {
        carregando(false);        
        atualizarListaPastas();
      }, 2000);
    } catch (error) {
      console.error('Falha ao clonar:', error);
      notificar('erro', 'Falha ao clonar');
    } finally {
      carregando(false);
    }
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
  };
</script>

<style scoped>
  .uppercase-input :deep(input) {
    text-transform: uppercase;
  }
</style>

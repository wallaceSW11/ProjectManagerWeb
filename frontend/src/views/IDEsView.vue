<template>
  <v-container>
    <v-row no-gutters>
      <v-col cols="12">
        <div class="d-flex justify-space-between">
          <div>
            <h1>IDEs</h1>
          </div>
        </div>
      </v-col>

      <v-col cols="12">
        <div class="d-flex justify-space-between align-center py-2">
          <BotaoTerciario
            @click="abrirModalParaNovo"
            texto="Adicionar"
            icone="mdi-plus"
          />
        </div>
      </v-col>

      <v-col cols="12">
        <v-row>
          <v-col
            v-for="ide in ides"
            :key="ide.identificador"
            cols="12"
            md="6"
            lg="4"
          >
            <v-card>
              <v-card-title>{{ ide.nome }}</v-card-title>
              <v-card-text>
                <div class="text-caption text-grey">Comando:</div>
                <code class="text-body-2">{{ ide.comandoParaExecutar }}</code>
              </v-card-text>
              <v-card-actions>
                <v-spacer />
                <v-btn
                  icon="mdi-pencil"
                  size="small"
                  variant="text"
                  @click="abrirModalParaEdicao(ide)"
                />
                <v-btn
                  icon="mdi-delete"
                  size="small"
                  variant="text"
                  color="error"
                  @click="excluirIDE(ide)"
                />
              </v-card-actions>
            </v-card>
          </v-col>

          <v-col
            v-if="ides.length === 0"
            cols="12"
          >
            <v-alert
              type="info"
              variant="tonal"
            >
              Nenhuma IDE cadastrada. Clique em "Adicionar IDE" para começar.
            </v-alert>
          </v-col>
        </v-row>
      </v-col>
    </v-row>

    <!-- Modal de Cadastro/Edição -->
    <ModalPadrao
      v-model="modalAberto"
      :titulo="modoEdicao ? 'Editar IDE' : 'Nova IDE'"
      :acaoBotaoPrimario="salvarIDE"
      :acaoBotaoSecundario="fecharModal"
    >
      <v-form ref="formRef">
        <v-text-field
          v-model="ideSelecionada.nome"
          label="Nome da IDE"
          placeholder="Ex: VS Code, Kiro, Delphi"
          :rules="[regras.obrigatorio]"
        />

        <v-text-field
          v-model="ideSelecionada.comandoParaExecutar"
          label="Comando para executar"
          placeholder="Ex: code ., kiro ."
          :rules="[regras.obrigatorio]"
        />

        <v-checkbox
          v-model="ideSelecionada.aceitaPerfilPersonalizado"
          label="Aceita perfil personalizado (--profile)"
          hide-details
        />
      </v-form>
    </ModalPadrao>
  </v-container>
</template>

<script setup lang="ts">
  import { onMounted, reactive, ref } from 'vue';
  import type { IIDE } from '@/types';
  import IDEModel from '@/models/IDEModel';
  import IDEsService from '@/services/IDEsService';
  import { carregandoAsync, notificar } from '@/utils/eventBus';

  const ides = reactive<IIDE[]>([]);
  const ideSelecionada = ref<IIDE>(new IDEModel());
  const modalAberto = ref(false);
  const modoEdicao = ref(false);
  const formRef = ref();

  const regras = {
    obrigatorio: (v: string) => !!v?.trim() || 'Campo obrigatório'
  };

  onMounted(async () => {
    await carregarIDEs();
  });

  const carregarIDEs = async (): Promise<void> => {
    try {
      const resposta = await carregandoAsync(async () => {
        return await IDEsService.getIDEs();
      });

      Object.assign(
        ides,
        resposta.map((i: any) => new IDEModel(i))
      );
    } catch (error) {
      console.error('Falha ao obter IDEs:', error);
      notificar('erro', 'Falha ao carregar IDEs');
    }
  };

  const abrirModalParaNovo = (): void => {
    modoEdicao.value = false;
    ideSelecionada.value = new IDEModel();
    modalAberto.value = true;
  };

  const abrirModalParaEdicao = (ide: IIDE): void => {
    modoEdicao.value = true;
    ideSelecionada.value = new IDEModel(ide);
    modalAberto.value = true;
  };

  const fecharModal = (): void => {
    modalAberto.value = false;
    ideSelecionada.value = new IDEModel();
  };

  const salvarIDE = async (): Promise<void> => {
    const { valid } = await formRef.value.validate();
    if (!valid) return;

    try {
      if (modoEdicao.value) {
        await IDEsService.atualizarIDE(ideSelecionada.value);
        const indice = ides.findIndex(
          i => i.identificador === ideSelecionada.value.identificador
        );
        if (indice !== -1) {
          Object.assign(ides[indice], ideSelecionada.value);
        }
        notificar('sucesso', 'IDE atualizada');
      } else {
        await IDEsService.adicionarIDE(ideSelecionada.value);
        ides.push(new IDEModel(ideSelecionada.value));
        notificar('sucesso', 'IDE criada');
      }

      modalAberto.value = false;
    } catch (error: any) {
      console.error('Falha ao salvar IDE:', error);
      notificar('erro', error.response?.data || 'Falha ao salvar IDE');
    }
  };

  const excluirIDE = async (ide: IIDE): Promise<void> => {
    const confirmacao = confirm(
      `Deseja realmente excluir a IDE "${ide.nome}"?`
    );

    if (!confirmacao) return;

    try {
      await IDEsService.excluirIDE(ide);
      const indice = ides.findIndex(i => i.identificador === ide.identificador);
      if (indice !== -1) {
        ides.splice(indice, 1);
      }
      notificar('sucesso', 'IDE excluída');
    } catch (error: any) {
      console.error('Falha ao excluir IDE:', error);
      const mensagem = error.response?.data || 'Falha ao excluir IDE';
      notificar('erro', mensagem);
    }
  };
</script>

<style scoped>
  code {
    background-color: rgba(0, 0, 0, 0.05);
    padding: 2px 6px;
    border-radius: 4px;
  }
</style>

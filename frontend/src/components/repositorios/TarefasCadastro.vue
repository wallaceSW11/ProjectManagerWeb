<template>
  <v-row no-gutters>
    <v-col cols="12">
      <v-text-field
        label="URL Base do Gestor de Tarefas"
        v-model="repositorio.urlBaseGestorTarefas"
        hint="Link do Jira, YouTrack, etc. (máx. 200 caracteres)"
        persistent-hint
        counter="200"
        clearable
      />
    </v-col>

    <v-col
      cols="12"
      class="mt-4"
    >
      <h3>Códigos de Tarefa</h3>
      <v-divider class="mb-2" />

      <div class="d-flex align-center mb-2">
        <v-btn
          variant="outlined"
          @click="iniciarCadastroCodigoTarefa"
        >
          <v-icon>mdi-plus</v-icon>
          Adicionar código
        </v-btn>
      </div>

      <!-- Modal de cadastro/edição de código de tarefa -->
      <ModalPadrao
        v-model="exibirModalCodigoTarefa"
        :titulo="
          codigoTarefaEditandoId
            ? 'Editar Código de Tarefa'
            : 'Cadastro de Código de Tarefa'
        "
        textoBotaoPrimario="Salvar"
        :acaoBotaoPrimario="salvarCodigoTarefa"
        :acaoBotaoSecundario="cancelarEdicaoCodigoTarefa"
        larguraMinima="700px"
      >
        <v-row>
          <v-col cols="5">
            <v-text-field
              ref="campoIniciais"
              v-model="codigoTarefaForm.iniciais"
              label="Iniciais"
              density="compact"
              variant="underlined"
            />
          </v-col>
          <v-col cols="7">
            <v-select
              v-model="codigoTarefaForm.pastaCentralizadora"
              label="Pasta centralizadora"
              :items="pastasCentralizadoras"
              density="compact"
              variant="underlined"
              clearable
              hint="Se vazio, usa a pasta do repositório"
              persistent-hint
            />
          </v-col>
        </v-row>
        <v-row>
          <v-col cols="4">
            <v-switch
              v-model="codigoTarefaForm.criarBranchRemoto"
              label="Criar branch remoto"
              color="primary"
              density="compact"
              hide-details
            />
          </v-col>
          <v-col cols="4">
            <v-switch
              v-model="codigoTarefaForm.clonarAgregados"
              label="Clonar agregados"
              color="primary"
              density="compact"
              hide-details
            />
          </v-col>
          <v-col cols="4">
            <v-switch
              v-model="codigoTarefaForm.baixarHistoricoCompleto"
              label="Histórico completo"
              color="primary"
              density="compact"
              hide-details
            />
          </v-col>
        </v-row>
        <v-row>
          <v-col cols="4">
            <v-switch
              v-model="codigoTarefaForm.habilitarTipos"
              label="Habilitar tipos"
              color="primary"
              density="compact"
              hide-details
            />
          </v-col>
        </v-row>
      </ModalPadrao>

      <v-data-table
        :items="repositorio.codigosTarefa || []"
        :headers="colunasCodigosTarefa"
        hide-default-footer
      >
        <template #[`item.criarBranchRemoto`]="{ item }">
          <v-icon
            v-if="item.criarBranchRemoto"
            color="success"
          >
            mdi-check
          </v-icon>
          <v-icon
            v-else
            color="grey"
          >
            mdi-close
          </v-icon>
        </template>
        <template #[`item.clonarAgregados`]="{ item }">
          <v-icon
            v-if="item.clonarAgregados"
            color="success"
          >
            mdi-check
          </v-icon>
          <v-icon
            v-else
            color="grey"
          >
            mdi-close
          </v-icon>
        </template>
        <template #[`item.habilitarTipos`]="{ item }">
          <v-icon
            v-if="item.habilitarTipos"
            color="success"
          >
            mdi-check
          </v-icon>
          <v-icon
            v-else
            color="grey"
          >
            mdi-close
          </v-icon>
        </template>
        <template #[`item.pastaCentralizadora`]="{ item }">
          <span v-if="item.pastaCentralizadora">
            {{ item.pastaCentralizadora }}
          </span>
          <span
            v-else
            class="text-grey"
          >
            (herdado do repo)
          </span>
        </template>
        <template #[`item.actions`]="{ item }">
          <IconeComTooltip
            icone="mdi-pencil"
            texto="Editar"
            :acao="() => editarCodigoTarefa(item)"
            top
          />
          <IconeComTooltip
            icone="mdi-delete"
            texto="Excluir"
            :acao="() => removerCodigoTarefa(item)"
            top
          />
        </template>
      </v-data-table>
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
  import { computed, ref, reactive, watch, nextTick } from 'vue';
  import type { IRepositorio, ICodigoTarefa } from '@/types';
  import { useConfiguracaoStore } from '@/stores/configuracao';
  import { notificar } from '@/utils/eventBus';
  import IconeComTooltip from '@/components/comum/botao/IconeComTooltip.vue';
  import ModalPadrao from '@/components/comum/ModalPadrao.vue';

  const repositorio = defineModel<IRepositorio>({ required: true });
  const configuracaoStore = useConfiguracaoStore();

  const pastasCentralizadoras = computed(
    () => configuracaoStore.pastasCentralizadoras?.map((p: any) => p.nome) || []
  );

  const exibirModalCodigoTarefa = ref(false);
  const codigoTarefaEditandoId = ref<string | null>(null);
  const campoIniciais = ref<any>(null);

  watch(exibirModalCodigoTarefa, async abriu => {
    if (abriu) {
      await nextTick();
      campoIniciais.value?.focus();
    }
  });
  const codigoTarefaForm = reactive<ICodigoTarefa>({
    identificador: crypto.randomUUID(),
    iniciais: '',
    branchPrincipal: 'develop',
    criarBranchRemoto: false,
    clonarAgregados: false,
    baixarHistoricoCompleto: false,
    habilitarTipos: false,
    tiposHabilitados: [],
    pastaCentralizadora: null
  });

  const colunasCodigosTarefa = [
    { title: 'Iniciais', key: 'iniciais', align: 'start' as const },
    { title: 'Pasta', key: 'pastaCentralizadora', align: 'start' as const },
    {
      title: 'Branch remoto',
      key: 'criarBranchRemoto',
      align: 'center' as const
    },
    { title: 'Agregados', key: 'clonarAgregados', align: 'center' as const },
    { title: 'Tipos', key: 'habilitarTipos', align: 'center' as const },
    {
      title: 'Ações',
      key: 'actions',
      align: 'center' as const,
      width: '200px'
    }
  ];

  const iniciarCadastroCodigoTarefa = (): void => {
    codigoTarefaForm.identificador = crypto.randomUUID();
    codigoTarefaForm.iniciais = '';
    codigoTarefaForm.branchPrincipal = 'develop';
    codigoTarefaForm.criarBranchRemoto = false;
    codigoTarefaForm.clonarAgregados = false;
    codigoTarefaForm.baixarHistoricoCompleto = false;
    codigoTarefaForm.habilitarTipos = false;
    codigoTarefaForm.pastaCentralizadora = null;
    codigoTarefaEditandoId.value = null;
    exibirModalCodigoTarefa.value = true;
  };

  const editarCodigoTarefa = (item: ICodigoTarefa): void => {
    codigoTarefaForm.identificador = item.identificador;
    codigoTarefaForm.iniciais = item.iniciais;
    codigoTarefaForm.branchPrincipal = item.branchPrincipal;
    codigoTarefaForm.criarBranchRemoto = item.criarBranchRemoto;
    codigoTarefaForm.clonarAgregados = item.clonarAgregados;
    codigoTarefaForm.baixarHistoricoCompleto = item.baixarHistoricoCompleto;
    codigoTarefaForm.habilitarTipos = item.habilitarTipos;
    codigoTarefaForm.tiposHabilitados = [...(item.tiposHabilitados || [])];
    codigoTarefaForm.pastaCentralizadora = item.pastaCentralizadora || null;
    codigoTarefaEditandoId.value = item.identificador;
    exibirModalCodigoTarefa.value = true;
  };

  const cancelarEdicaoCodigoTarefa = (): void => {
    exibirModalCodigoTarefa.value = false;
    codigoTarefaEditandoId.value = null;
  };

  const salvarCodigoTarefa = (): void => {
    if (!codigoTarefaForm.iniciais.trim()) {
      alert('Iniciais são obrigatórias');
      return;
    }

    // Verifica duplicidade (ignora se for edição do mesmo código)
    const duplicado = (repositorio.value.codigosTarefa || []).some(
      (c: ICodigoTarefa) =>
        c.iniciais === codigoTarefaForm.iniciais.trim().toUpperCase() &&
        c.identificador !== codigoTarefaEditandoId.value
    );
    if (duplicado) {
      alert('Já existe um código de tarefa com essas iniciais');
      return;
    }

    codigoTarefaForm.iniciais = codigoTarefaForm.iniciais.trim().toUpperCase();

    if (codigoTarefaEditandoId.value) {
      const indice = repositorio.value.codigosTarefa.findIndex(
        (c: ICodigoTarefa) => c.identificador === codigoTarefaEditandoId.value
      );
      if (indice !== -1) {
        repositorio.value.codigosTarefa.splice(indice, 1, {
          ...codigoTarefaForm
        });
      }
    } else {
      if (!repositorio.value.codigosTarefa) {
        repositorio.value.codigosTarefa = [];
      }
      repositorio.value.codigosTarefa.push({ ...codigoTarefaForm });
    }

    exibirModalCodigoTarefa.value = false;
    codigoTarefaEditandoId.value = null;
    notificar('sucesso', 'Código de tarefa salvo');
  };

  const removerCodigoTarefa = (item: ICodigoTarefa): void => {
    const confirmado = confirm(
      `Deseja remover o código de tarefa "${item.iniciais}"?`
    );
    if (!confirmado) return;

    repositorio.value.codigosTarefa = repositorio.value.codigosTarefa.filter(
      (c: ICodigoTarefa) => c.identificador !== item.identificador
    );
    notificar('sucesso', 'Código de tarefa removido');
  };
</script>

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

    <v-col cols="12" class="mt-4">
      <h3>Códigos de Tarefa</h3>
      <v-divider class="mb-2" />

      <div v-if="!editandoCodigoTarefa" class="d-flex align-center mb-2">
        <v-text-field
          v-model="novoCodigoTarefa.iniciais"
          label="Iniciais"
          density="compact"
          variant="outlined"
          hide-details
          class="mr-2"
          style="max-width: 120px;"
        />
        <v-btn variant="outlined" @click="iniciarCadastroCodigoTarefa">
          <v-icon>mdi-plus</v-icon>
          Adicionar
        </v-btn>
      </div>

      <v-card v-if="editandoCodigoTarefa" class="pa-4 mb-3" variant="outlined">
        <v-row>
          <v-col cols="4">
            <v-text-field
              v-model="codigoTarefaForm.iniciais"
              label="Iniciais"
              density="compact"
              variant="outlined"
              hide-details
            />
          </v-col>
          <v-col cols="4" class="d-flex align-center">
            <v-switch
              v-model="codigoTarefaForm.criarBranchRemoto"
              label="Criar branch remoto"
              density="compact"
              hide-details
            />
          </v-col>
        </v-row>
        <v-row>
          <v-col cols="4">
            <v-switch
              v-model="codigoTarefaForm.clonarAgregados"
              label="Clonar agregados"
              density="compact"
              hide-details
            />
          </v-col>
          <v-col cols="4">
            <v-switch
              v-model="codigoTarefaForm.baixarHistoricoCompleto"
              label="Histórico completo"
              density="compact"
              hide-details
            />
          </v-col>
          <v-col cols="4">
            <v-switch
              v-model="codigoTarefaForm.habilitarTipos"
              label="Habilitar tipos"
              density="compact"
              hide-details
            />
          </v-col>
        </v-row>
        <v-row class="mt-2">
          <v-col cols="12" class="d-flex justify-end">
            <v-btn variant="outlined" class="mr-2" @click="cancelarEdicaoCodigoTarefa">
              Cancelar
            </v-btn>
            <v-btn color="primary" @click="salvarCodigoTarefa">
              Salvar
            </v-btn>
          </v-col>
        </v-row>
      </v-card>

      <v-data-table
        :items="repositorio.codigosTarefa || []"
        :headers="colunasCodigosTarefa"
        hide-default-footer
      >
        <template #[`item.criarBranchRemoto`]="{ item }">
          <v-icon v-if="item.criarBranchRemoto" color="success">mdi-check</v-icon>
          <v-icon v-else color="grey">mdi-close</v-icon>
        </template>
        <template #[`item.clonarAgregados`]="{ item }">
          <v-icon v-if="item.clonarAgregados" color="success">mdi-check</v-icon>
          <v-icon v-else color="grey">mdi-close</v-icon>
        </template>
        <template #[`item.habilitarTipos`]="{ item }">
          <v-icon v-if="item.habilitarTipos" color="success">mdi-check</v-icon>
          <v-icon v-else color="grey">mdi-close</v-icon>
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
  import { ref, reactive } from 'vue';
  import type { IRepositorio, ICodigoTarefa } from '@/types';
  import { notificar } from '@/utils/eventBus';
  import IconeComTooltip from '@/components/comum/botao/IconeComTooltip.vue';

  const repositorio = defineModel<IRepositorio>({ required: true });

  const editandoCodigoTarefa = ref(false);
  const codigoTarefaEditandoId = ref<string | null>(null);
  const novoCodigoTarefa = reactive({ iniciais: '' });
  const codigoTarefaForm = reactive<ICodigoTarefa>({
    identificador: crypto.randomUUID(),
    iniciais: '',
    branchPrincipal: 'develop',
    criarBranchRemoto: false,
    clonarAgregados: false,
    baixarHistoricoCompleto: false,
    habilitarTipos: false,
    tiposHabilitados: [],
  });

  const colunasCodigosTarefa = [
    { title: 'Iniciais', key: 'iniciais', align: 'start' as const },
    { title: 'Branch remoto', key: 'criarBranchRemoto', align: 'center' as const },
    { title: 'Agregados', key: 'clonarAgregados', align: 'center' as const },
    { title: 'Tipos', key: 'habilitarTipos', align: 'center' as const },
    { title: 'Ações', key: 'actions', align: 'center' as const, width: '200px' },
  ];

  const iniciarCadastroCodigoTarefa = (): void => {
    if (!novoCodigoTarefa.iniciais.trim()) {
      alert('Informe as iniciais do código de tarefa');
      return;
    }

    const jaExiste = (repositorio.value.codigosTarefa || []).some(
      (c: ICodigoTarefa) => c.iniciais === novoCodigoTarefa.iniciais.trim()
    );
    if (jaExiste) {
      alert('Já existe um código de tarefa com essas iniciais');
      return;
    }

    codigoTarefaForm.identificador = crypto.randomUUID();
    codigoTarefaForm.iniciais = novoCodigoTarefa.iniciais.trim().toUpperCase();
    codigoTarefaForm.branchPrincipal = 'develop';
    codigoTarefaForm.criarBranchRemoto = false;
    codigoTarefaForm.clonarAgregados = false;
    codigoTarefaForm.baixarHistoricoCompleto = false;
    codigoTarefaForm.habilitarTipos = false;
    codigoTarefaEditandoId.value = null;
    editandoCodigoTarefa.value = true;
    novoCodigoTarefa.iniciais = '';
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
    codigoTarefaEditandoId.value = item.identificador;
    editandoCodigoTarefa.value = true;
  };

  const cancelarEdicaoCodigoTarefa = (): void => {
    editandoCodigoTarefa.value = false;
    codigoTarefaEditandoId.value = null;
  };

  const salvarCodigoTarefa = (): void => {
    if (!codigoTarefaForm.iniciais.trim()) {
      alert('Iniciais são obrigatórias');
      return;
    }

    if (codigoTarefaEditandoId.value) {
      const indice = repositorio.value.codigosTarefa.findIndex(
        (c: ICodigoTarefa) => c.identificador === codigoTarefaEditandoId.value
      );
      if (indice !== -1) {
        repositorio.value.codigosTarefa.splice(indice, 1, { ...codigoTarefaForm });
      }
    } else {
      if (!repositorio.value.codigosTarefa) {
        repositorio.value.codigosTarefa = [];
      }
      repositorio.value.codigosTarefa.push({ ...codigoTarefaForm });
    }

    editandoCodigoTarefa.value = false;
    codigoTarefaEditandoId.value = null;
    notificar('sucesso', 'Código de tarefa salvo');
  };

  const removerCodigoTarefa = (item: ICodigoTarefa): void => {
    const confirmado = confirm(`Deseja remover o código de tarefa "${item.iniciais}"?`);
    if (!confirmado) return;

    repositorio.value.codigosTarefa = repositorio.value.codigosTarefa.filter(
      (c: ICodigoTarefa) => c.identificador !== item.identificador
    );
    notificar('sucesso', 'Código de tarefa removido');
  };
</script>

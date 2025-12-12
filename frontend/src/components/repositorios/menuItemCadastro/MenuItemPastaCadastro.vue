<template>
  <v-col cols="12">
    <div
      :class="[
        'd-flex align-center',
        emModoInicial ? 'justify-space-between' : 'justify-end',
      ]"
      style="height: 70px"
    >
      <div>
        <v-btn
          @click="
            () => (emModoInicial ? mudarParaCadastro() : salvarAlteracoes())
          "
        >
          <v-icon>{{ emModoInicial ? 'mdi-plus' : 'mdi-check' }}</v-icon>
          {{ emModoInicial ? 'Adicionar' : 'Salvar' }}
        </v-btn>

        <v-btn
          v-if="emModoCadastroEdicao"
          variant="plain"
          class="ml-2"
          @click="descartarAlteracoes"
        >
          <v-icon>mdi-cancel</v-icon>
          Cancelar
        </v-btn>
      </div>
    </div>

    <v-tabs-window v-model="pagina">
      <v-tabs-window-item>
        <v-data-table
          :headers="colunas"
          :items="menu.pastas"
          hide-default-footer
        >
          <template #[`item.actions`]="{ item }">
            <IconeComTooltip
              icone="mdi-pencil"
              texto="Editar"
              :acao="() => mudarParaEdicao(item)"
              top
            />
            <IconeComTooltip
              icone="mdi-delete"
              texto="Excluir"
              :acao="() => excluirPasta(item)"
              top
            />
          </template>
        </v-data-table>
      </v-tabs-window-item>

      <v-tabs-window-item>
        <v-form ref="formPasta">
          <v-text-field
            label="Pasta Origem"
            v-model="pastaSelecionada.origem"
            :rules="obrigatorio"
            hint="Caminho completo da pasta que será copiada (ex: C:\__Arquivos_Wallace__\AmazonQ\kiro\.kiro)"
            persistent-hint
          />
          <v-text-field
            label="Destino"
            v-model="pastaSelecionada.destino"
            :rules="obrigatorio"
            hint="Onde colar a pasta, relativo ao repositório (ex: bimerup\frontend\bimerup)"
            persistent-hint
          />
        </v-form>
      </v-tabs-window-item>
    </v-tabs-window>
  </v-col>
</template>

<script setup>
  import { computed, reactive, ref } from 'vue';
  import MenuModel from '../../../models/MenuModel';
  import PastaMenuModel from '@/models/PastaMenuModel';

  const menu = defineModel(new MenuModel());

  const obrigatorio = [v => !!v || 'Obrigatório'];

  const pagina = ref(0);

  const colunas = reactive([
    { title: 'Origem', key: 'origem', align: 'start' },
    { title: 'Destino', key: 'destino', align: 'start' },
    { title: 'Actions', key: 'actions', align: 'center', width: '200px' },
  ]);

  const pastaSelecionada = reactive(new PastaMenuModel());

  const irParaCadastro = () => (pagina.value = 1);

  const mudarParaEdicao = item => {
    Object.assign(pastaSelecionada, item);
    modoOperacao.value = MODO_OPERACAO.EDICAO.valor;
    irParaCadastro();
  };

  const MODO_OPERACAO = {
    INICIAL: {
      titulo: 'Adicionar',
      valor: 'ADICIONAR',
    },
    NOVO: {
      titulo: 'Novo',
      valor: 'NOVO',
    },
    EDICAO: {
      titulo: 'Editar',
      valor: 'EDITAR',
    },
  };

  let modoOperacao = ref(MODO_OPERACAO.INICIAL.valor);

  const emModoInicial = computed(
    () => modoOperacao.value === MODO_OPERACAO.INICIAL.valor
  );
  const emModoCadastro = computed(
    () => modoOperacao.value === MODO_OPERACAO.NOVO.valor
  );
  const emModoEdicao = computed(
    () => modoOperacao.value === MODO_OPERACAO.EDICAO.valor
  );
  const emModoCadastroEdicao = computed(
    () => emModoCadastro.value || emModoEdicao.value
  );

  const formPasta = ref(null);

  const formularioPastaValido = async () => {
    const resposta = await formPasta.value.validate();

    return resposta.valid;
  };

  const mudarParaCadastro = () => {
    modoOperacao.value = MODO_OPERACAO.NOVO.valor;
    limparCampos();
    irParaCadastro();
  };
  
  const salvarAlteracoes = async () => {
    if (!(await formularioPastaValido())) return;

    try {
      emModoCadastro.value ? adicionarMenuItem() : atualizarMenuItem();
      irParaListagem();
    } catch (error) {
      console.error('Falha ao salvar alteracoes do cadastro:', error);
    }
  };

  const adicionarMenuItem = () => {
    menu.value.pastas.push(new PastaMenuModel(pastaSelecionada));
  };

  const atualizarMenuItem = () => {
    const indice = menu.value.pastas.findIndex(
      p => p.identificador === pastaSelecionada.identificador
    );

    indice !== -1 &&
      Object.assign(
        menu.value.pastas[indice],
        new PastaMenuModel(pastaSelecionada)
      );
  };

  const excluirPasta = item => {
    const confirmDelete = confirm(`Deseja remover a pasta "${item.origem}"?`);

    if (!confirmDelete) return;

    menu.value.pastas = menu.value.pastas.filter(
      p => p.identificador !== item.identificador
    );
  };

  const limparCampos = () => {
    Object.assign(pastaSelecionada, new PastaMenuModel());
  };

  const descartarAlteracoes = () => {
    limparCampos();
    irParaListagem();
  };

  const irParaListagem = () => {
    pagina.value = 0;
    modoOperacao.value = MODO_OPERACAO.INICIAL.valor;
  };
</script>

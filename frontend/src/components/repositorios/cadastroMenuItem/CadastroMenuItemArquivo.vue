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
          <v-icon>{{ emModoInicial ? "mdi-plus" : "mdi-check" }}</v-icon>
          {{ emModoInicial ? "Adicionar" : "Salvar" }}
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
          :items="menu.arquivos"
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
              :acao="() => excluirProjeto(item)"
              top
            />
          </template>
        </v-data-table>
      </v-tabs-window-item>

      <v-tabs-window-item>
        <v-form ref="formProjeto">
          <v-text-field
            label="Arquivo"
            v-model="arquivoSelecionado.arquivo"
            :rules="obrigatorio"
          />
          <v-text-field
            label="Destino"
            v-model="arquivoSelecionado.destino"
          />
          <v-checkbox
            label="Ignorar no git diff"
            v-model="arquivoSelecionado.ignorarGit"
          />
        </v-form>
      </v-tabs-window-item>
    </v-tabs-window>
  </v-col>
</template>

<script setup>
import { computed, reactive, ref } from "vue";
import ProjetoModel from "../../../models/ProjetoModel";
import MenuModel from "../../../models/MenuModel";
import ArquivoModel from "@/models/ArquivoModel";

const menu = defineModel(new MenuModel());

const obrigatorio = [(v) => !!v || "Obrigatório"];

const pagina = ref(0);

const colunas = reactive([
  { title: "Arquivo", key: "arquivo", align: "start" },
  { title: "Destino", key: "destino", align: "start" },
  { title: "Ignorar Git Diff", key: "ignorarGit", align: "start" },
  { title: "Actions", key: "actions", align: "center", width: "200px" },
]);


const arquivoSelecionado = reactive(new ArquivoModel());

const irParaCadastro = () => (pagina.value = 1);

const mudarParaEdicao = (item) => {
  Object.assign(arquivoSelecionado, item);
  modoOperacao.value = MODO_OPERACAO.EDICAO.valor;
  irParaCadastro();
};

const MODO_OPERACAO = {
  INICIAL: {
    titulo: "Adicionar",
    valor: "ADICIONAR",
  },
  NOVO: {
    titulo: "Novo",
    valor: "NOVO",
  },
  EDICAO: {
    titulo: "Editar",
    valor: "EDITAR",
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

const formProjeto = ref(null);

const formularioProjetoValido = async () => {
  const resposta = await formProjeto.value.validate();

  return resposta.valid;
};


const mudarParaCadastro = () => {
  modoOperacao.value = MODO_OPERACAO.NOVO.valor;
  limparCampos();
  irParaCadastro();
};
const salvarAlteracoes = async () => {
  if (!(await formularioProjetoValido())) return;

  try {
    emModoCadastro.value ? adicionarMenuItem() : atualizarMenuItem();
    irParaListagem();
  } catch (error) {
    console.error("Falha ao salvar alteracoes do cadastro:", error);
  }
};

const adicionarMenuItem = () => {
  menu.value.arquivos.push(new ArquivoModel(arquivoSelecionado));
};


const atualizarMenuItem = () => {
  const indice = menu.value.arquivos.findIndex(
    (p) => p.identificador === arquivoSelecionado.identificador
  );

  indice !== -1 &&
    Object.assign(menu.value.arquivos[indice], new ArquivoModel(arquivoSelecionado));
};

const excluirProjeto = (item) => {
  const confirmDelete = confirm(`Deseja remover o projeto "${item.nome}"?`);

  if (!confirmDelete) return;

  menu.value.arquivos = menu.value.arquivos.filter(
    (p) => p.identificador !== item.identificador
  );
};

const limparCampos = () => {
  Object.assign(arquivoSelecionado, new ProjetoModel());
};

const descartarAlteracoes = () => {
  // Perguntar sobre perder alteracoes
  limparCampos();
  irParaListagem();
};

const irParaListagem = () => {
  pagina.value = 0;
  modoOperacao.value = MODO_OPERACAO.INICIAL.valor;
};
</script>

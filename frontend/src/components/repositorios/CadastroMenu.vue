<template>
  <v-row no-gutters>
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
            :items="repositorio.menus"
            hide-default-footer
          >
            <template #[`item.actions`]="{ item }">
              <v-btn icon @click="mudarParaEdicao(item)"
                ><v-icon>mdi-pencil</v-icon></v-btn
              >
              <v-btn icon @click="excluirProjeto(item)"
                ><v-icon>mdi-delete</v-icon></v-btn
              >
            </template>
          </v-data-table>
        </v-tabs-window-item>

        <v-tabs-window-item>
          <v-form ref="formProjeto">
            <v-text-field
              label="Título"
              v-model="menuSelecionado.titulo"
              :rules="obrigatorio"
            />
            <v-select
              label="Tipo"
              :items="[{ titulo: 'Aplicar arquivos', valor: 'APLICAR_ARQUIVO'}]"
              v-model="menuSelecionado.tipo"
              item-value="valor"
              item-title="titulo"
            />
          </v-form>

          <CadastroMenuItem v-model="menuSelecionado" />
        </v-tabs-window-item>
      </v-tabs-window>
    </v-col>
  </v-row>
</template>

<script setup>
import { computed, reactive, ref } from "vue";
import RepositorioModel from "../../models/RepositorioModel";
import ProjetoModel from "../../models/ProjetoModel";
import MenuModel from "../../models/MenuModal";
import CadastroMenuItem from "./CadastroMenuItem.vue";

const repositorio = defineModel(new RepositorioModel());

const obrigatorio = [(v) => !!v || "Obrigatório"];

const pagina = ref(0);

const colunas = reactive([
  { title: "Título", key: "titulo", align: "start" },
  { title: "Tipo", key: "tipo.titulo", align: "start" },
  { title: "Actions", key: "actions", align: "center", width: "200px" },
]);

const menuSelecionado = reactive(new MenuModel());

const irParaCadastro = () => (pagina.value = 1);

const mudarParaEdicao = (item) => {
  console.log(". ~ item:", item);
  Object.assign(menuSelecionado, item);
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
    emModoCadastro.value ? adicionarMenu() : atualizarProjeto();
    irParaListagem();
  } catch (error) {
    console.error("Falha ao salvar alteracoes do cadastro:", error);
  }
};

const adicionarMenu = () => {
  repositorio.value.menus.push(new MenuModel(menuSelecionado));
};

const atualizarProjeto = () => {
  const indice = repositorio.value.menus.findIndex(
    (p) => p.id === menuSelecionado.id
  );

  indice !== -1 &&
    Object.assign(repositorio.value.menus[indice], menuSelecionado);
};

const excluirProjeto = (item) => {
  const confirmDelete = confirm(`Deseja remover o projeto "${item.nome}"?`);

  if (!confirmDelete) return;

  repositorio.value.menus = repositorio.value.menus.filter(
    (p) => p.id !== item.id
  );
};

const limparCampos = () => {
  Object.assign(menuSelecionado, new ProjetoModel());
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

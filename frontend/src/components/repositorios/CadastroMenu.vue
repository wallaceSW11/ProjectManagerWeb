<template>
  <v-row no-gutters>
    <v-col cols="12">
      <div>
        <v-btn @click="prepararParaCadastro()" class="mb-4">
          <v-icon>mdi-plus</v-icon>
          Adicionar
        </v-btn>
      </div>

      <div>
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
      </div>

      <div>
        <ModalPadrao
          v-model="exibirModalCadastroMenu"
          titulo="Cadastro de Menu de Contexto"
          :textoBotaoPrimario="emModoCadastro ? 'Adicionar' : 'Salvar'"
          textoBotaoSecundario="Cancelar"
          :acaoBotaoPrimario="salvarAlteracoes"
          :acaoBotaoSecundario="descartarAlteracoes"
          larguraMinima="800px"
        >
          <v-form ref="formProjeto">
            <v-text-field
              label="Título"
              v-model="menuSelecionado.titulo"
              :rules="obrigatorio"
            />
            <v-select
              label="Tipo"
              :items="TIPOS_MENU"
              v-model="menuSelecionado.tipo"
              item-value="valor"
              item-title="titulo"
            />

            <CadastroMenuItemArquivo v-model="menuSelecionado" v-if="menuSelecionado.tipo === 'APLICAR_ARQUIVO'" />
            <CadastroMenuItemComandoAvulso v-if="menuSelecionado.tipo === 'COMANDO_AVULSO'" v-model="menuSelecionado" />
          </v-form>
        </ModalPadrao>
      </div>
    </v-col>
  </v-row>
</template>

<script setup>
import { computed, reactive, ref } from "vue";
import RepositorioModel from "@/models/RepositorioModel";
import MenuModel from "@/models/MenuModel";
import ModalPadrao from "@/components/comum/ModalPadrao.vue";
import CadastroMenuItemArquivo from "@/components/repositorios/cadastroMenuItem/CadastroMenuItemArquivo.vue";
import CadastroMenuItemComandoAvulso from "@/components/repositorios/cadastroMenuItem/CadastroMenuItemComandoAvulso.vue";

const repositorio = defineModel(new RepositorioModel());

const obrigatorio = [(v) => !!v || "Obrigatório"];

const exibirModalCadastroMenu = ref(false);

const colunas = reactive([
  { title: "Título", key: "titulo", align: "start" },
  { title: "Tipo", key: "tipo", align: "start" },
  { title: "Actions", key: "actions", align: "center", width: "200px" },
]);

const TIPOS_MENU = reactive([
  { titulo: "Aplicar arquivos", valor: "APLICAR_ARQUIVO" },
  { titulo: "Comando avulso", valor: "COMANDO_AVULSO" }
]);

const menuSelecionado = reactive(new MenuModel());

const abrirModalCadastroMenu = () => (exibirModalCadastroMenu.value = true);

const mudarParaEdicao = (item) => {
  Object.assign(menuSelecionado, item);
  modoOperacao.value = MODO_OPERACAO.EDICAO.valor;
  abrirModalCadastroMenu();
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

const prepararParaCadastro = () => {
  modoOperacao.value = MODO_OPERACAO.NOVO.valor;
  limparCampos();
  abrirModalCadastroMenu();
};
const salvarAlteracoes = async () => {
  if (!(await formularioProjetoValido())) return;

  try {
    emModoCadastro.value ? adicionarMenu() : atualizarProjeto();
    descartarAlteracoes();
  } catch (error) {
    console.error("Falha ao salvar alteracoes do cadastro:", error);
  }
};

const adicionarMenu = () => {
  repositorio.value.menus.push(new MenuModel(menuSelecionado));
};

const atualizarProjeto = () => {
  const indice = repositorio.value.menus.findIndex(
    (p) => p.identificador === menuSelecionado.identificador
  );

  indice !== -1 &&
    Object.assign(repositorio.value.menus[indice], menuSelecionado);
};

const excluirProjeto = (item) => {
  const confirmDelete = confirm(`Deseja remover o projeto "${item.nome}"?`);

  if (!confirmDelete) return;

  repositorio.value.menus = repositorio.value.menus.filter(
    (p) => p.identificador !== item.identificador
  );
};

const limparCampos = () => {
  Object.assign(menuSelecionado, new MenuModel());
};

const descartarAlteracoes = () => {
  // Perguntar sobre perder alteracoes
  limparCampos();
  modoOperacao.value = MODO_OPERACAO.INICIAL.valor;
  exibirModalCadastroMenu.value = false;
};
</script>

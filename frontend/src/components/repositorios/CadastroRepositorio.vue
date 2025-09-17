<template>
  <v-form ref="formRepositorio">
    <v-row no-gutters>
      <v-col cols="12">
        <v-text-field
          label="Nome"
          v-model="repositorio.nome"
          :rules="obrigatorio"
        />
      </v-col>

      <v-col cols="12">
        <v-text-field
          label="Url"
          v-model="repositorio.url"
          :rules="obrigatorio"
        />
      </v-col>
    </v-row>
  </v-form>
</template>

<script setup>
import { computed, reactive, ref } from "vue";
import RepositorioModel from "../../models/RepositorioModel";
import ProjetoModel from "../../models/ProjetoModel";

const repositorio = defineModel(new RepositorioModel());

const obrigatorio = [(v) => !!v || "Obrigatório"];

const pagina = ref(0);

const salvarProjeto = () => {};

const colunas = reactive([
  { title: "Nome", key: "nome", align: "start" },
  { title: "Subdiretorio", key: "subdiretorio", align: "start" },
  { title: "Perfil VS Code", key: "perfilVSCode", align: "start" },
  { title: "Actions", key: "actions", align: "center", width: "200px" },
]);

const campos = [
  { model: "nome", component: "v-text-field", props: { label: "Nome", rules: [(v) => !!v || 'Obrigatório'] } },
  { model: "subdiretorio", component: "v-text-field", props: { label: "Subdiretório" } },
  { model: "perfilVSCode", component: "v-text-field", props: { label: "Perfil VS Code" } },
  { model: "comandos.instalar", component: "v-text-field", props: { label: "Instalar" } },
  { model: "comandos.iniciar", component: "v-text-field", props: { label: "Iniciar" } },
  { model: "comandos.buildar", component: "v-text-field", props: { label: "Buildar" } },
  { model: "comandos.abrirNoVSCode", component: "v-checkbox", props: { label: "Abrir no VS Code" } }
];

const projetoSelecionado = reactive(new ProjetoModel());

const irParaCadastro = () => (pagina.value = 1);

const mudarParaEdicao = (item) => {
  Object.assign(projetoSelecionado, item);
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

const adicionarProjeto = () => {
  repositorio.value.projetos.push(new ProjetoModel(projetoSelecionado));
};

const mudarParaCadastro = () => {
  modoOperacao.value = MODO_OPERACAO.NOVO.valor;
  limparCampos();
  irParaCadastro();
};
const salvarAlteracoes = async () => {
  if (!(await formularioProjetoValido())) return;

  try {
    emModoCadastro.value ? adicionarProjeto() : atualizarProjeto();
    irParaListagem();
  } catch (error) {
    console.error('Falha ao salvar alteracoes do cadastro:', error);
  }

};

const atualizarProjeto = () => {
  const indice = repositorio.value.projetos.findIndex(
    (p) => p.id === projetoSelecionado.id
  );

  (indice !== -1) && Object.assign(repositorio.value.projetos[indice], projetoSelecionado);
};

const excluirProjeto = (item) => {
  const confirmDelete = confirm(`Deseja remover o projeto "${item.nome}"?`);

  if (!confirmDelete) return;

  repositorio.value.projetos = repositorio.value.projetos.filter(
    (p) => p.id !== item.id
  );
};

const limparCampos = () => {
  Object.assign(projetoSelecionado, new ProjetoModel());
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

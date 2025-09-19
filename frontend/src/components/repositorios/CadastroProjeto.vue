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
          :items="repositorio.projetos"
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
            label="Nome"
            v-model="projetoSelecionado.nome"
            :rules="obrigatorio"
          />
          <v-text-field
            label="Subdiretório"
            v-model="projetoSelecionado.subdiretorio"
          />
          <v-text-field
            label="Perfil VS Code"
            v-model="projetoSelecionado.perfilVSCode"
          />

          <h2>Comandos:</h2>
          <v-divider />

          <v-text-field
            label="Instalar"
            v-model="projetoSelecionado.comandos.instalar"
          />
          <v-text-field
            label="Iniciar"
            v-model="projetoSelecionado.comandos.iniciar"
          />
          <v-text-field
            label="Buildar"
            v-model="projetoSelecionado.comandos.buildar"
          />
          <v-checkbox
            label="Abrir no VS Code"
            v-model="projetoSelecionado.comandos.abrirNoVSCode"
          />
        </v-form>
      </v-tabs-window-item>
    </v-tabs-window>
  </v-col>
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
  {
    model: "nome",
    component: "v-text-field",
    props: { label: "Nome", rules: [(v) => !!v || "Obrigatório"] },
  },
  {
    model: "subdiretorio",
    component: "v-text-field",
    props: { label: "Subdiretório" },
  },
  {
    model: "perfilVSCode",
    component: "v-text-field",
    props: { label: "Perfil VS Code" },
  },
  {
    model: "comandos.instalar",
    component: "v-text-field",
    props: { label: "Instalar" },
  },
  {
    model: "comandos.iniciar",
    component: "v-text-field",
    props: { label: "Iniciar" },
  },
  {
    model: "comandos.buildar",
    component: "v-text-field",
    props: { label: "Buildar" },
  },
  {
    model: "comandos.abrirNoVSCode",
    component: "v-checkbox",
    props: { label: "Abrir no VS Code" },
  },
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
    console.error("Falha ao salvar alteracoes do cadastro:", error);
  }
};

const atualizarProjeto = () => {
  const indice = repositorio.value.projetos.findIndex(
    (p) => p.identificador === projetoSelecionado.identificador
  );

  indice !== -1 &&
    Object.assign(repositorio.value.projetos[indice], projetoSelecionado);
};

const excluirProjeto = (item) => {
  const confirmDelete = confirm(`Deseja remover o projeto "${item.nome}"?`);

  if (!confirmDelete) return;

  repositorio.value.projetos = repositorio.value.projetos.filter(
    (p) => p.identificador !== item.identificador
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

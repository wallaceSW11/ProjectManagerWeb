<template>
  <v-col cols="12">
    <div>
      <BotaoTerciario texto="Adicionar" icone="mdi-plus" @click="prepararParaCadastro"  class="my-2"/>
    </div>

    <div>
      <v-data-table
        :headers="colunas"
        :items="repositorio.projetos"
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
    </div>

    <div>
      <ModalPadrao
        v-model="exibirModalCadastroProjeto"
        titulo="Cadastro de Projeto"
        :textoBotaoPrimario="emModoCadastro ? 'Adicionar' : 'Salvar'"
        textoBotaoSecundario="Cancelar"
        :acaoBotaoPrimario="salvarAlteracoes"
        :acaoBotaoSecundario="descartarAlteracoes"
      >
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
          <v-select
            :items="configuracaoStore.perfisVSCode"
            label="Perfil VS Code"
            v-model="projetoSelecionado.perfilVSCode"
            item-title="nome"
            item-value="nome"
          />

          <v-text-field
            label="Comando para abrir o arquivo coverage"
            v-model="projetoSelecionado.arquivoCoverage"
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
      </ModalPadrao>
    </div>
  </v-col>
</template>

<script setup>
import { reactive, ref } from "vue";
import RepositorioModel from "../../models/RepositorioModel";
import ProjetoModel from "../../models/ProjetoModel";
import { useConfiguracaoStore } from "@/stores/configuracao";
import BotaoTerciario from "../comum/botao/BotaoTerciario.vue";
import { useModoOperacao } from "@/composables/useModoOperacao";

const repositorio = defineModel(new RepositorioModel());
const configuracaoStore = useConfiguracaoStore();
const obrigatorio = [(v) => !!v || "Obrigatório"];
const exibirModalCadastroProjeto = ref(false);

const {
  emModoCadastro,
  definirModoInicial,
  definirModoCadastro,
  definirModoEdicao
} = useModoOperacao();

const colunas = reactive([
  { title: "Nome", key: "nome", align: "start" },
  { title: "Subdiretorio", key: "subdiretorio", align: "start" },
  { title: "Perfil VS Code", key: "perfilVSCode", align: "start" },
  { title: "Actions", key: "actions", align: "center", width: "200px" },
]);

const projetoSelecionado = reactive(new ProjetoModel());

const abrirModalCadastroProjeto = () => (exibirModalCadastroProjeto.value = true);

const mudarParaEdicao = (item) => {
  Object.assign(projetoSelecionado, item);
  definirModoEdicao();
  abrirModalCadastroProjeto();
};

const formProjeto = ref(null);

const formularioProjetoValido = async () => {
  const resposta = await formProjeto.value.validate();

  return resposta.valid;
};

const adicionarProjeto = () => {
  repositorio.value.projetos.push(new ProjetoModel(projetoSelecionado));
};

const prepararParaCadastro = () => {
  definirModoCadastro();
  limparCampos();
  abrirModalCadastroProjeto();
};

const salvarAlteracoes = async () => {
  if (!(await formularioProjetoValido())) return;

  try {
    emModoCadastro.value ? adicionarProjeto() : atualizarProjeto();
    descartarAlteracoes();
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
  definirModoInicial();
  exibirModalCadastroProjeto.value = false;
};
</script>

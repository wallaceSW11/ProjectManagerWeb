<template>
  <v-row no-gutters>
    <v-col cols="12">
      <div>
        <BotaoTerciario
          texto="Adicionar"
          icone="mdi-plus"
          @click="prepararParaCadastro"
          class="my-2"
        />
      </div>

      <div>
        <v-data-table
          :headers="colunas"
          :items="repositorio.menus"
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
          v-model="exibirModalMenuCadastro"
          titulo="Cadastro de Menu de Contexto"
          :textoBotaoPrimario="emModoCadastro ? 'Adicionar' : 'Salvar'"
          :acaoBotaoPrimario="() => salvarAlteracoes()"
          :acaoBotaoSecundario="() => descartarAlteracoes()"
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

            <MenuItemArquivoCadastro
              v-model="menuSelecionado"
              v-if="menuSelecionado.tipo === 'APLICAR_ARQUIVO'"
            />
            <MenuItemComandoAvulsoCadastro
              v-if="menuSelecionado.tipo === 'COMANDO_AVULSO'"
              v-model="menuSelecionado"
            />
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
import MenuItemArquivoCadastro from "@/components/repositorios/menuItemCadastro/MenuItemArquivoCadastro.vue";
import MenuItemComandoAvulsoCadastro from "@/components/repositorios/menuItemCadastro/MenuItemComandoAvulsoCadastro.vue";
import BotaoTerciario from "../comum/botao/BotaoTerciario.vue";
import { useModoOperacao } from "@/composables/useModoOperacao";

const {
  emModoCadastro,
  definirModoCadastro,
  definirModoEdicao,
  definirModoInicial,
} = useModoOperacao();

const repositorio = defineModel(new RepositorioModel());

const obrigatorio = [(v) => !!v || "Obrigatório"];

const exibirModalMenuCadastro = ref(false);

const colunas = reactive([
  { title: "Título", key: "titulo", align: "start" },
  { title: "Tipo", key: "tipo", align: "start" },
  { title: "Actions", key: "actions", align: "center", width: "200px" },
]);

const TIPOS_MENU = reactive([
  { titulo: "Aplicar arquivos", valor: "APLICAR_ARQUIVO" },
  { titulo: "Comando avulso", valor: "COMANDO_AVULSO" },
]);

const menuSelecionado = reactive(new MenuModel());

const prepararParaCadastro = () => {
  definirModoCadastro();
  limparCampos();
  abrirModalMenuCadastro();
};

const abrirModalMenuCadastro = () => {
  exibirModalMenuCadastro.value = true;
};

const mudarParaEdicao = (item) => {
  Object.assign(menuSelecionado, item);
  definirModoEdicao();
  abrirModalMenuCadastro();
};

const formProjeto = ref(null);

const formularioProjetoValido = async () => {
  const resposta = await formProjeto.value.validate();

  return resposta.valid;
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
  definirModoInicial();
  exibirModalMenuCadastro.value = false;
};
</script>

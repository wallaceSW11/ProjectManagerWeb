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
          :acaoBotaoPrimario="() => metodoBotaoPrimario()"
          :acaoBotaoSecundario="() => metodoBotaoSecundario()"
          larguraMinima="800px"
        >
          <v-tabs-window v-model="paginaMenu">
            <v-tabs-window-item>
              <v-form ref="formProjeto">
                <v-text-field
                  label="Título"
                  v-model="menuSelecionado.titulo"
                  :rules="obrigatorio"
                />

                <div>
                  <div>
                    <BotaoTerciario
                      texto="Adicionar"
                      icone="mdi-plus"
                      @click="prepararParaCadastroArquivos"
                      class="my-2"
                    />
                  </div>
                  <div>
                    <v-data-table
                      :headers="colunasMenuArquivos"
                      :items="menuSelecionado.arquivos"
                      hide-default-footer
                    >
                      <template #[`item.actions`]="{ item }">
                        <IconeComTooltip
                          icone="mdi-pencil"
                          texto="Editar"
                          :acao="() => mudarParaEdicaoArquivo(item)"
                          top
                        />
                        <IconeComTooltip
                          icone="mdi-delete"
                          texto="Excluir"
                          :acao="() => excluirArquivo(item)"
                          top
                        />
                      </template>
                    </v-data-table>
                  </div>
                </div>
              </v-form>
            </v-tabs-window-item>

            <v-tabs-window-item>
              <v-form ref="formArquivo">
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
        </ModalPadrao>
      </div>
    </v-col>
  </v-row>
</template>

<script setup>
import { computed, reactive, ref } from "vue";
import RepositorioModel from "@/models/RepositorioModel";
import MenuModel from "@/models/MenuModel";
import BotaoTerciario from "../comum/botao/BotaoTerciario.vue";
import { useModoOperacao } from "@/composables/useModoOperacao";
import ArquivoModel from "@/models/ArquivoModel";

const {
  emModoCadastro,
  definirModoCadastro,
  definirModoEdicao,
  definirModoInicial,
} = useModoOperacao();

const repositorio = defineModel(new RepositorioModel());
const menuSelecionado = reactive(new MenuModel());
const arquivoSelecionado = reactive(new ArquivoModel());
const arquivoEmEdicao = ref(false);

const obrigatorio = [(v) => !!v || "Obrigatório"];

const exibirModalMenuCadastro = ref(false);
const paginaMenu = ref(0);

const colunas = reactive([
  { title: "Título", key: "titulo", align: "start" },
  { title: "Tipo", key: "tipo", align: "start" },
  { title: "Actions", key: "actions", align: "center", width: "200px" },
]);

const colunasMenuArquivos = reactive([
  { title: "Arquivo", key: "arquivo", align: "start" },
  { title: "Destino", key: "destino", align: "start" },
  { title: "Ignorar Git Diff", key: "ignorarGit", align: "start" },
  { title: "Actions", key: "actions", align: "center", width: "200px" },
]);

const paginaTabela = computed(() => paginaMenu.value === 0);

const metodoBotaoPrimario = computed(() => {
  return paginaTabela.value ? salvarAlteracoes : salvarAlteracoesArquivos;
});

const metodoBotaoSecundario = computed(() => {
  return paginaTabela.value ? descartarAlteracoes : descartarAlteracoesArquivos;
});

const mudarParaPaginaTabela = () => (paginaMenu.value = 0);

const descartarAlteracoesArquivos = () => {
  mudarParaPaginaTabela();
  limparCamposArquivos();
};

const prepararParaCadastro = () => {
  definirModoCadastro();
  limparCampos();
  abrirModalMenuCadastro();
};

const prepararParaCadastroArquivos = () => {
  paginaMenu.value = 1;
  limparCamposArquivos();
};

const limparCamposArquivos = () => {
  Object.assign(arquivoSelecionado, new ArquivoModel());
};

const abrirModalMenuCadastro = () => {
  exibirModalMenuCadastro.value = true;
};

const mudarParaEdicaoArquivo = (item) => {
  Object.assign(arquivoSelecionado, item);
  arquivoEmEdicao.value = true;
  paginaMenu.value = 1;
};

const mudarParaEdicao = (item) => {
  Object.assign(menuSelecionado, item);
  definirModoEdicao();
  abrirModalMenuCadastro();
};

const formProjeto = ref(null);
const formArquivo = ref(null);

const formularioProjetoValido = async () => {
  const resposta = await formProjeto.value.validate();

  return resposta.valid;
};

const formularioArquivoValido = async () => {
  const resposta = await formArquivo.value.validate();

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

const salvarAlteracoesArquivos = async () => {
  if (!(await formularioArquivoValido())) return;

  if (arquivoEmEdicao.value) {
    const indice = menuSelecionado.arquivos.findIndex(
      (a) => a.identificador === arquivoSelecionado.identificador
    );

    if (indice !== -1) {
      Object.assign(menuSelecionado.arquivos[indice], arquivoSelecionado);
    }

    arquivoEmEdicao.value = false;
    limparCamposArquivos();
    mudarParaPaginaTabela();
    return;
  }

  menuSelecionado.arquivos.push(new ArquivoModel(arquivoSelecionado));  
  
  mudarParaPaginaTabela();
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

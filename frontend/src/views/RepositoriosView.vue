<template>
  <v-container>
    <v-row no-gutters>
      <v-col cols="12">
        <div class="d-flex justify-space-between">
          <div>
            <h1>Repositórios</h1>
          </div>
        </div>
      </v-col>

      <v-col cols="12">
        <div class="d-flex justify-space-between align-center">
          <BotaoTerciario v-if="!emModoCadastroEdicao" @click="mudarParaCadastro()" texto="Adicionar" icone="mdi-plus" />

          <div v-if="emModoInicial" style="width: 300px">
            <v-text-field
              placeholder="Pesquise pelo nome"
              append-inner-icon="mdi-magnify"
            />
          </div>
        </div>
      </v-col>

      <v-col cols="12">
        <div>
          <v-tabs-window v-model="pagina" class="altura-limitada">
            <v-tabs-window-item>
              <ListaRepositorios
                :itens="repositorios"
                @editar="mudarParaEdicao"
                @excluir="excluirRepositorio"
              />
            </v-tabs-window-item>

            <v-tabs-window-item>
              <v-tabs v-model="paginaCadastro">
                <v-tab>Geral</v-tab>
                <v-tab>Projetos</v-tab>
                <v-tab>Menu de contexo</v-tab>
              </v-tabs>

              <v-tabs-window v-model="paginaCadastro">
                <v-tabs-window-item>
                  <CadastroRepositorio v-model="repositorioSelecionado" :repositorios="repositorios" class="pt-4"/>
                </v-tabs-window-item>
                <v-tabs-window-item>
                  <CadastroProjeto v-model="repositorioSelecionado" />
                </v-tabs-window-item>
                <v-tabs-window-item>
                  <CadastroMenu v-model="repositorioSelecionado" class="pt-4"/>
                </v-tabs-window-item>
              </v-tabs-window>
            </v-tabs-window-item>
          </v-tabs-window>
        </div>
      </v-col>

      <v-col cols="12" v-if="!emModoInicial">
        <div class="d-flex align-center justify-end">
          <div>
            <BotaoPrimario @click="salvarAlteracoes" texto="Salvar" icone="mdi-check" />
            <BotaoSecundario @click="descartarAlteracoes" texto="Cancelar" icone="mdi-cancel" />
          </div>
        </div>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import ListaRepositorios from "../components/repositorios/ListaRepositorios.vue";
import CadastroRepositorio from "../components/repositorios/CadastroRepositorio.vue";
import RepositorioModel from "../models/RepositorioModel";
import RepositoriosService from "../services/RepositoriosService";
import CadastroMenu from "@/components/repositorios/CadastroMenu.vue";
import CadastroProjeto from "../components/repositorios/CadastroProjeto.vue";
import { carregandoAsync, notificar } from "@/utils/eventBus";

let repositorios = reactive([]);
const repositorioSelecionado = reactive(new RepositorioModel());

const obterRepositorios = async () => {
  try {
    const resposta = await carregandoAsync(async () => {
      return await RepositoriosService.getRepositorios();
    })

    Object.assign(
      repositorios,
      resposta.map((r) => new RepositorioModel(r))
    );
  } catch (error) {
    console.error("Falha ao obter os relatorios:", error);
  }
};

onMounted(async () => {
  obterRepositorios();
});

const pagina = ref(0);
const paginaCadastro = ref(0);

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

const irParaListagem = () => {
  pagina.value = 0;
  modoOperacao.value = MODO_OPERACAO.INICIAL.valor;
};
const irParaCadastro = () => {
  paginaCadastro.value = 0;
  pagina.value = 1;
};

const mudarParaEdicao = (identificador) => {
  let repo = repositorios.find((r) => r.identificador === identificador);

  if (!repo) {
    alert("Repositorio nao encontrado");

    return;
  }

  modoOperacao.value = MODO_OPERACAO.EDICAO.valor;
  Object.assign(repositorioSelecionado, repo);
  irParaCadastro();
};

const mudarParaCadastro = () => {
  modoOperacao.value = MODO_OPERACAO.NOVO.valor;
  limparCampos();
  irParaCadastro();
};

const formularioValido = () => {
  return true;
};

const salvarAlteracoes = () => {
  if (!formularioValido()) return;

  try {
    emModoCadastro.value ? criarRepositorio() : atualizarRepositorio();
    irParaListagem();
  } catch (error) {
    console.error("Falha ao salvar alteracoes: ", error);
  }
};

const criarRepositorio = async () => {
  try {
    await RepositoriosService.adicionarRepositorio(repositorioSelecionado);
    repositorios.push(new RepositorioModel(repositorioSelecionado));
    limparCampos();
    notificar("sucesso", "Repositorio criado");
  } catch (error) {
    console.error("Falha ao criar repositorio: " + error);
    notificar("erro", "Falha ao criar repositorio");
  }
};

const atualizarRepositorio = async () => {
  try {
    await RepositoriosService.atualizarRepositorio(repositorioSelecionado);
    
    const indice = repositorios.findIndex(
      (r) => r.identificador === repositorioSelecionado.identificador
    );
    indice !== -1 &&
      Object.assign(repositorios[indice], repositorioSelecionado);

    notificar("sucesso", "Repositorio atualizado");
  } catch (error) {
    console.error("Falha ao criar repositorio" + error);
    notificar("erro", "Falha ao criar repositorio");
  }
};

const excluirRepositorio = async (item) => {
  const confirmDelete = confirm(`Deseja excluir o repositorio "${item.nome}"?`);

  if (!confirmDelete) return;

  try {
    await RepositoriosService.excluirRepositorio(item);
    const indice = repositorios.findIndex((r) => r.identificador === item.identificador);
    indice !== -1 && repositorios.splice(indice, 1);

    notificar("sucesso", "Repositorio excluido");
  } catch (error) {
    console.error("Falha ao excluir repositorio" + error);
    notificar("erro", "Falha ao excluir repositorio");
  }
};

const descartarAlteracoes = () => {
  // Perguntar sobre perder alteracoes
  limparCampos();
  irParaListagem();
};

const limparCampos = () => {
  Object.assign(repositorioSelecionado, new RepositorioModel());
};
</script>

<style scoped>
.altura-limitada {
  height: calc(100dvh - 220px);
  overflow: auto;
}
</style>

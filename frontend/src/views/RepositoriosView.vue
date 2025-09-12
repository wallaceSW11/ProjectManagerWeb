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
        <div :class="['d-flex align-center', emModoInicial ? 'justify-space-between' : 'justify-end']" style="height: 70px;">
          <div>
            <v-btn @click="() => emModoInicial ? mudarParaCadastro() : salvarAlteracoes()">
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

          <div v-if="emModoInicial" style="width: 300px;">
            <v-text-field  placeholder="Pesquise pelo nome" append-inner-icon="mdi-magnify" />
          </div>
        </div>

        <v-tabs-window v-model="pagina">
          <v-tabs-window-item>
            <ListaRepositorios :itens="repositorios" @editar="mudarParaEdicao" />
          </v-tabs-window-item>

          <v-tabs-window-item>
            <CadastroRepositorio v-model="repositorioSelecionado" />
          </v-tabs-window-item>
        </v-tabs-window>
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

const repositorios = reactive([]);
const repositorioSelecionado = reactive(new RepositorioModel());

const obterRepositorios = async () => {
  try {
    const resposta = await RepositoriosService.getRepositorios();

    Object.assign(
      repositorios,
      resposta.map((r) => new RepositorioModel(r))
    );
  } catch (error) {
    console.error("Falha ao obter os relatorios:", error);
  }
}

onMounted(async () => {
  obterRepositorios();
});


const pagina = ref(0);

const MODO_OPERACAO = {
  INICIAL: {
    titulo: "Adicionar",
    valor: "ADICIONAR"
  },
  NOVO: {
    titulo: "Novo",
    valor: "NOVO"
  },
  EDICAO: {
    titulo: "Editar",
    valor: "EDITAR"
  }
};


let modoOperacao = ref(MODO_OPERACAO.INICIAL.valor);

const emModoInicial = computed(() => modoOperacao.value === MODO_OPERACAO.INICIAL.valor);
const emModoCadastro = computed(() => modoOperacao.value === MODO_OPERACAO.NOVO.valor);
const emModoEdicao = computed(() => modoOperacao.value === MODO_OPERACAO.EDICAO.valor);
const emModoCadastroEdicao = computed(() => emModoCadastro.value || emModoEdicao.value);

const irParaListagem = () => {
  pagina.value = 0;
  modoOperacao.value = MODO_OPERACAO.INICIAL.valor;
};
const irParaCadastro = () => pagina.value = 1;

const mudarParaEdicao = (identificador) => {
  let repo = repositorios.find(r => r.id === identificador);

  if (!repo) {
    alert('Repositorio nao encontrado');

    return;
  }

  modoOperacao.value = MODO_OPERACAO.EDICAO.valor;
  Object.assign(repositorioSelecionado, repo);
  irParaCadastro();
};

const mudarParaCadastro = () => {
  modoOperacao.value = MODO_OPERACAO.NOVO.valor;
  console.log(`aqui`, modoOperacao.value);
  irParaCadastro();
};

const formularioValido = () => {
  return true;
}

const salvarAlteracoes = () => {
  if (!formularioValido()) return;

  emModoCadastro
    ? criarRepositorio()
    : atualizarRepositorio();

  irParaListagem();
};

const criarRepositorio = () => {

};

const atualizarRepositorio = () => {

};

const descartarAlteracoes = () => {
  // Perguntar sobre perder alteracoes
  limparCampos();
  irParaListagem();
};

const limparCampos = () => {
  Object.assign(repositorioSelecionado, new RepositorioModel());
}
</script>

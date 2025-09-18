<template>
  <v-overlay
      v-model="exibirCarregando"
      class="d-flex flex-column align-center justify-center text-center"
      persistent
      scrim="#121212dd"
      opacity=".9"
    >
      <div class="bg-surface rounded-xl px-8 py-6 d-flex flex-column align-center">
        <div class="d-flex align-center mb-3">
          <img :src="logo" width="24px" height="24px" />
          <h2 class="ml-2text-h5 font-weight-bold">Project Manager Web</h2>
        </div>

        <v-progress-circular
          indeterminate
          color="primary"
          size="32"
          width="6"
          class="mb-4"
        />

        <span class="text-subtitle-1 font-italic">
          {{ mensagem }}
        </span>
      </div>
    </v-overlay>

  <v-app>
    <v-app-bar>
      <v-app-bar-title>
        <img :src="logo" width="20px"/>
            Project Manager Web
        </v-app-bar-title>

      <div>
        <v-btn class="text-none" @click="exibirModalClone = true">
          <v-icon class="pr-2" color="primary">mdi-git</v-icon>
          Clonar
        </v-btn>
        <v-btn class="text-none" :to="{ name: 'pastas' }">
          <v-icon class="pr-2" color="primary">mdi-folder</v-icon>
          Pastas
        </v-btn>
        <v-btn class="text-none" :to="{ name: 'repositorios' }">
          <v-icon class="pr-2" color="primary">mdi-source-repository</v-icon>
          Repositórios
        </v-btn>
        <v-btn icon :to="{ name: 'configuracao' }">
          <v-icon class="pr-2" color="primary">mdi-cog</v-icon>
        </v-btn>

        <v-btn>
          <v-icon color="primary">mdi-calendar</v-icon>
          <v-tooltip
            activator="parent"
            location="left"
          >{{ `Compilado em: ${compiladoEm}` }}</v-tooltip>
        </v-btn>
      </div>
    </v-app-bar>

    <v-main>
      <router-view />
    </v-main>
  </v-app>

  <div>
    <v-dialog v-model="exibirModalClone" max-width="500">
      <v-card>
        <v-card-title> Clonar </v-card-title>

        <v-card-text class="pt-0">
          <v-form ref="formClone">
            <v-text-field
              label="Diretório"
              v-model="clone.diretorioRaiz"
              :rules="obrigatorio"
            />

            <v-select
              label="Repositório"
              :items="repositorios"
              item-title="nome"
              item-value="id"
              v-model="clone.repositorioId"
              :rules="obrigatorio"
            />

            <v-text-field
              label="Branch"
              v-model="clone.branch"
              :rules="obrigatorio"
            />

            <v-checkbox
              label="Clonar agregados"
              hide-details
              v-model="clone.baixarAgregados"
            />
            <v-checkbox
              label="Criar branch remoto"
              hide-details
              v-model="clone.criarBranchRemoto"
            />

            <v-text-field
              label="Número da tarefa"
              v-model="clone.codigo"
              :rules="obrigatorio"
            />
            <v-text-field
              label="Descrição"
              v-model="clone.descricao"
              :rules="obrigatorio"
            />

            <v-radio-group
              label="Tipo"
              inline
              hide-details
              v-model="clone.tipo"
            >
              <v-radio label="Nenhum" value="nenhum" />
              <v-radio label="Feature" value="feature" />
              <v-radio label="Bug" value="bug" />
              <v-radio label="HotFix" value="hotfix" />
            </v-radio-group>
          </v-form>
        </v-card-text>

        <v-card-actions>
          <v-spacer />
          <v-btn color="primary" variant="outlined" @click="clonar()">
            <v-icon>mdi-download</v-icon>
            Clonar
          </v-btn>
          <v-btn @click="fecharClone">
            <v-icon>mdi-close</v-icon>
            Fechar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { onBeforeUnmount, onMounted, reactive, ref } from "vue";
import RepositorioModel from "./models/RepositorioModel";
import RepositoriosService from "./services/RepositoriosService";
import CloneModel from "./models/CloneModel";
import ConfiguracaoModel from "./models/ConfiguracaoModel";
import ConfiguracaoService from "./services/ConfiguracaoService";
import CloneService from "./services/CloneService";
import logo from "@/assets/logo.svg"
import VersaoService from "./services/VersaoService";
import { useRoute } from "vue-router";

let exibirModalClone = ref(false);
const repositorios = reactive([]);
const configuracao = reactive(new ConfiguracaoModel());
const clone = reactive(new CloneModel());
const compiladoEm = ref();
const route = useRoute();

onMounted(async () => {
  await consultarRepositorios();
  await consultarConfiguracao();
  await consultarVersao();

  clone.diretorioRaiz = configuracao.diretorioRaiz + "\\";
});

const consultarRepositorios = async () => {
  try {
    let response = await RepositoriosService.getRepositorios();
    Object.assign(
      repositorios,
      response.map((r) => new RepositorioModel(r))
    );
  } catch (error) {
    console.log("Falha ao consultar os repositorios", error);
  }
};

const consultarConfiguracao = async () => {
  try {
    const response = await ConfiguracaoService.getConfiguracao();
    Object.assign(configuracao, new ConfiguracaoModel(response));
  } catch (error) {
    console.error("Falha ao consultar as configurações:", error);
  }
};

const consultarVersao = async () => {
  try {
    const response = await carregandoAsync(async () => {
      const res = await VersaoService.obterVersao()

      return res
    }, 'Consultando a versão...');

    compiladoEm.value = response
  } catch (error) {
    console.error("Falha ao consultar a versão:", error)
  }
}

const formClone = ref(null);
const obrigatorio = [(v) => !!v || "Obrigatório"];

const formularioValido = async () => {
  const form = await formClone.value.validate();

  return form.valid;
};

const clonar = async () => {
  if (!(await formularioValido())) return;

  try {
    await CloneService.clonar(clone);
    exibirModalClone.value = false;
    route.go(0);
  } catch (error) {
    console.error("Falha ao clonar:", error);
  }
};

const fecharClone = () => {
  exibirModalClone.value = false;
  Object.assign(clone, new CloneModel());
}



import eventBus, { carregandoAsync } from '@/utils/eventBus'
const exibirCarregando = ref(true)
const mensagem = ref('Carregando...')

const handleCarregando = ({ exibir, texto }) => {
  exibirCarregando.value = exibir
  mensagem.value = texto
}

onMounted(() => {
  eventBus.on('carregando', handleCarregando)
})

onBeforeUnmount(() => {
  eventBus.off('carregando', handleCarregando)
})
</script>

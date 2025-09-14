<template>
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
              v-model="clone.gitId"
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
          <v-btn>
            <v-icon>mdi-close</v-icon>
            Fechar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from "vue";
import RepositorioModel from "./models/RepositorioModel";
import RepositoriosService from "./services/RepositoriosService";
import CloneModel from "./models/CloneModel";
import ConfiguracaoModel from "./models/ConfiguracaoModel";
import ConfiguracaoService from "./services/ConfiguracaoService";
import CloneService from "./services/CloneService";
import logo from "@/assets/logo.svg"

let exibirModalClone = ref(false);
const repositorios = reactive([]);
const configuracao = reactive(new ConfiguracaoModel());
const clone = reactive(new CloneModel());

onMounted(async () => {
  await consultarRepositorios();
  await consultarConfiguracao();

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

const formClone = ref(null);
const obrigatorio = [(v) => !!v || "Obrigatório"];

const formularioValido = async () => {
  const form = await formClone.value.validate();

  return form.valid;
};

const clonar = async () => {
  if (!(await formularioValido())) return;

  console.log(clone);

  try {
    await CloneService.clonar(clone);
    exibirModalClone.value = false;
  } catch (error) {
    console.error("Falha ao clonar:", error);
  }
};
</script>

<style>
/* Add global styles here if needed */
</style>

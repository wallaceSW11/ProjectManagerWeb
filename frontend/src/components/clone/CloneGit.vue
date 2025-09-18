<template>
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
</template>

<script setup>
import CloneModel from "@/models/CloneModel";
import ConfiguracaoModel from "@/models/ConfiguracaoModel";
import RepositorioModel from "@/models/RepositorioModel";
import ConfiguracaoService from "@/services/ConfiguracaoService";
import RepositoriosService from "@/services/RepositoriosService";
import { notificar } from "@/utils/eventBus";
import { onMounted, reactive, ref } from "vue";

const clone = reactive(new CloneModel());
const repositorios = reactive([]);
const configuracao = reactive(new ConfiguracaoModel());


const exibirModalClone = defineModel(false);

onMounted(async () => {
  await Promise.all([consultarRepositorios(), consultarConfiguracao()]);

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
    notificar('erro', "Falha ao consultar os repositorios", error.message);
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

  try {
    await CloneService.clonar(clone);
    exibirModalClone.value = false;
    Object.assign(clone, new CloneModel());
    clone.diretorioRaiz = configuracao.diretorioRaiz + "\\";
  } catch (error) {
    console.error("Falha ao clonar:", error);
  }
};

const fecharClone = () => {
  exibirModalClone.value = false;
  Object.assign(clone, new CloneModel());
};
</script>

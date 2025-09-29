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
            item-value="identificador"
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
            :disabled="
              !repositorios.find((r) => r.identificador === clone.repositorioId)
                ?.agregados?.length
            "
          />
          <v-checkbox
            label="Criar branch remoto"
            hide-details
            v-model="clone.criarBranchRemoto"
          />

          <v-radio-group label="Tipo" inline hide-details v-model="clone.tipo" :disabled="!clone.criarBranchRemoto">
            <v-radio label="Nenhum" value="nenhum" />
            <v-radio label="Feature" value="feature" />
            <v-radio label="Bug" value="bug" />
            <v-radio label="HotFix" value="hotfix" />
          </v-radio-group>

          <v-text-field
            label="Número da tarefa"
            v-model.uppercase="clone.codigo"
            class="uppercase-input"
            :rules="obrigatorio"
          />
          <v-text-field
            label="Descrição"
            v-model="clone.descricao"
            :rules="obrigatorio"
          />

          
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
import CloneService from "@/services/CloneService";
import { onMounted, reactive, ref, watch } from "vue";
import { useConfiguracaoStore } from "@/stores/configuracao";
import RepositoriosService from "@/services/RepositoriosService";
import RepositorioModel from "@/models/RepositorioModel";
import { atualizarListaPastas, carregando, notificar } from "@/utils/eventBus";

const clone = reactive(new CloneModel());
const repositorios = reactive([]);
const configuracaoStore = useConfiguracaoStore();
const exibirModalClone = defineModel(false);


onMounted(() => {
  clone.diretorioRaiz = configuracaoStore.diretorioRaiz + "\\";
});

watch(exibirModalClone, async (novoValor) => {
  if (novoValor)  await consultarRepositorios();
});

const consultarRepositorios = async () => {
  try {
    const resposta = await RepositoriosService.getRepositorios();
    Object.assign(repositorios, resposta.map(r => new RepositorioModel(r)))
  } catch (error) {
    console.error('falha')
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
    clone.codigo = clone.codigo.toUpperCase();
    await CloneService.clonar(clone);
    exibirModalClone.value = false;
    Object.assign(clone, new CloneModel());
    clone.diretorioRaiz = configuracaoStore.diretorioRaiz + "\\";
    
    carregando(true, "Clonando...");
    setTimeout(() => {
      carregando(false);
      notificar("sucesso", "Clone iniciado");
      atualizarListaPastas();
    }, 2000);
  } catch (error) {
    console.error("Falha ao clonar:", error);
    notificar("erro", "Falha ao clonar");
  } finally {
    carregando(false);
  }
};

const fecharClone = () => {
  exibirModalClone.value = false;
  Object.assign(clone, new CloneModel());
};
</script>

<style scoped>
.uppercase-input :deep(input) {
  text-transform: uppercase;
}
</style>

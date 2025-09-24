<template>
  <v-dialog v-model="exibirModalPasta" max-width="500">
    <v-card>
      <v-card-title>Cadastrar pasta</v-card-title>

      <v-card-text class="pt-0">
        <v-form ref="formPasta">
          <v-text-field
            label="Diretório"
            v-model="pasta.diretorio"
            :rules="obrigatorio"
            disabled
          />

          <v-select
            label="Repositório"
            :items="repositorios"
            item-title="nome"
            item-value="identificador"
            v-model="pasta.repositorioId"
            :rules="obrigatorio"
          />

          <v-text-field
            label="Branch"
            v-model="pasta.branch"
            :rules="obrigatorio"
          />

          <v-text-field
            label="Número da tarefa"
            v-model.uppercase="pasta.codigo"
            class="uppercase-input"
            :rules="obrigatorio"
          />
          <v-text-field
            label="Descrição"
            v-model="pasta.descricao"
            :rules="obrigatorio"
          />

          <v-radio-group label="Tipo" inline hide-details v-model="pasta.tipo">
            <v-radio label="Nenhum" value="nenhum" />
            <v-radio label="Feature" value="feature" />
            <v-radio label="Bug" value="bug" />
            <v-radio label="HotFix" value="hotfix" />
          </v-radio-group>
        </v-form>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn color="primary" variant="outlined" @click="criar()">
          <v-icon>mdi-plus</v-icon>
          Criar
        </v-btn>
        <v-btn @click="fecharPasta">
          <v-icon>mdi-close</v-icon>
          Fechar
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import PastaModel from "@/models/PastaModel";
import PastaService from "@/services/PastasService";
import { onMounted, reactive, ref, watch } from "vue";
import { useConfiguracaoStore } from "@/stores/configuracao";
import RepositoriosService from "@/services/RepositoriosService";
import RepositorioModel from "@/models/RepositorioModel";
import { notificar, atualizarListaPastas } from "@/utils/eventBus";

const pasta = reactive(new PastaModel());
const repositorios = reactive([]);
const configuracaoStore = useConfiguracaoStore();
const exibirModalPasta = defineModel(false);
const props = defineProps({
  pasta: {
    type: Object,
    default: () => new PastaModel(),
  },
});

onMounted(async () => {
  await consultarRepositorios();
});

watch(exibirModalPasta, (novoValor) => {
  if (!novoValor) return;

  pasta.diretorio = props.pasta.diretorio;

  const { codigo, descricao } = obterCodigoDescricao();
  pasta.codigo = codigo;
  pasta.descricao = descricao;
});

const obterCodigoDescricao = () => {
  const diretorio = pasta.diretorio.replace(
    configuracaoStore.diretorioRaiz + "\\",
    ""
  );

  if (!diretorio) return { codigo: "", descricao: "" };

  // Procura padrão: letras/números/hífen + underscore + resto
  const comUnderscore = /^([A-Z0-9-]+)_(.+)$/i;
  const match = diretorio.match(comUnderscore);

  if (match) {
    return {
      codigo: match[1].toUpperCase(),
      descricao: match[2].replace(/_/g, " "),
    };
  }

  // Se não tiver underscore, tenta separar por padrão de código
  const soCodigo = /^([A-Z]+\d*-?\d*)(.*)$/i;
  const matchCodigo = texto.match(soCodigo);

  if (matchCodigo && matchCodigo[2]) {
    return {
      codigo: matchCodigo[1].toUpperCase(),
      descricao: matchCodigo[2],
    };
  }

  // Fallback: tudo como descrição
  return {
    codigo: "",
    descricao: texto,
  };
};

const consultarRepositorios = async () => {
  try {
    const resposta = await RepositoriosService.getRepositorios();
    Object.assign(
      repositorios,
      resposta.map((r) => new RepositorioModel(r))
    );
  } catch (error) {
    console.error("falha");
  }
};

const formPasta = ref(null);
const obrigatorio = [(v) => !!v || "Obrigatório"];

const formularioValido = async () => {
  const form = await formPasta.value.validate();

  return form.valid;
};

const criar = async () => {
  if (!(await formularioValido())) return;

  try {
    await PastaService.criar(pasta);
    exibirModalPasta.value = false;
    Object.assign(pasta, new PastaModel());
    notificar("sucesso", "Pasta cadastrada");
    atualizarListaPastas();
  } catch (error) {
    console.error("Falha ao criar pasta:", error);
    notificar("erro", "Falha ao criar pasta");
  }
};

const fecharPasta = () => {
  exibirModalPasta.value = false;
  Object.assign(pasta, new PastaModel());
};
</script>

<style scoped>
.uppercase-input :deep(input) {
  text-transform: uppercase;
}
</style>

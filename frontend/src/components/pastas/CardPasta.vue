<template>
  <v-card
    :class="[
      'mb-4 mr-2',
      {
        'card-selecionado': pastaSelecionada.diretorio === pasta.diretorio,
      },
    ]"
    @click="selecionarPasta(pasta)"
  >
    <v-card-title>
      <div class="d-flex justify-space-between">
        <div>
          {{ descricaoPasta(pasta) }}
        </div>

        <div v-if="!pasta.identificador">
          <IconeComTooltip
            icone="mdi-plus"
            texto="Cadastrar pasta"
            :acao="() => exibirCadastroPasta(pasta)"
            top
          />
        </div>

        <div v-if="pasta.menus.length > 0">
          <v-menu location="bottom">
            <template #activator="{ props }">
              <v-btn v-bind="props" icon size="small" variant="flat">
                <v-icon small>mdi-dots-vertical</v-icon>
              </v-btn>
            </template>

            <v-list dense>
              <v-list-item
                v-for="menu in pasta.menus"
                :key="menu.identificador"
                @click="executarMenu(pasta, menu.identificador)"
              >
                <v-list-item-title>
                  <v-icon>mdi-folder</v-icon>
                  {{ menu.titulo }}
                </v-list-item-title>
              </v-list-item>
            </v-list>
          </v-menu>
        </div>
      </div>
    </v-card-title>

    <v-card-text>
      <v-row no-gutters>
        <v-col cols="12" class="pb-1">
          <v-icon>mdi-folder</v-icon>
          {{ pasta.diretorio }}
        </v-col>

        <v-col cols="12" class="pt-1">
          <span class="pr-2">Tipo:</span>
          <v-icon :color="dadosTipo.cor">{{ dadosTipo.icone }}</v-icon>
          {{ dadosTipo.titulo }}
        </v-col>
      </v-row>
    </v-card-text>
  </v-card>
</template>

<script setup>
import { computed } from 'vue';


const props = defineProps({
  pasta: {
    type: Object,
    required: true,
  },
  pastaSelecionada: {
    type: Object,
    required: true,
  },
});

const emit = defineEmits(["selecionarPasta", "exibirCadastroPasta", "executarMenu"]);

const selecionarPasta = (pasta) => {
  emit("selecionarPasta", pasta);
};

const exibirCadastroPasta = (pasta) => {
  emit("exibirCadastroPasta", pasta);
};

const executarMenu = (pasta, menuId) => {
  emit("executarMenu", pasta, menuId);
};

const descricaoPasta = (pasta) => {
  return pasta.codigo
    ? `${pasta.codigo} - ${pasta.descricao}`
    : pasta.descricao;
};


const TIPOS = {
  NENHUM: {
    icone: "mdi-set-none",
    titulo: "Nenhum",
    cor: "grey",
  },
  FEATURE: {
    icone: "mdi-creation",
    titulo: "Melhoria",
    cor: "green",
  },
  BUG: {
    icone: "mdi-bug",
    titulo: "Erro",
    cor: "red",
  },
  HOTFIX: {
    icone: "mdi-ambulance",
    titulo: "Hotfix",
    cor: "purple",
  },
}

const dadosTipo = computed(() => {
  console.log("Tipo da pasta:", props.pasta.tipo);
  return TIPOS[props.pasta.tipo.toUpperCase()] || TIPOS.NENHUM;
});

</script>

<style scoped>
.card-selecionado {
  border: 1px solid rgb(var(--v-theme-primary));
}
</style>
<template>
  <ModalPadrao
    v-model="exibir"
    titulo="Sites IIS"
    :exibir-botao-secundario="false"
    texto-botao-primario="Fechar"
    :acao-botao-primario="() => (exibir = false)"
  >
    <v-card
      v-for="site in sites"
      :key="site.nome"
      class="mb-4"
    >
      <v-card-title>
        <v-icon>mdi-web</v-icon>
        <span class="pl-2">{{ site.nome }}</span>
      </v-card-title>

      <v-card-text class="pt-2">
        <v-icon>mdi-gate</v-icon>
        <span class="pl-2">Porta: </span>
        <span>{{ site.porta }}</span>
      </v-card-text>

      <v-card-actions class="d-flex justify-end">
        <IconeComTooltip
          icone="mdi-play"
          texto="Iniciar"
          :acao="() => iniciarSite(site)"
          :desabilitado="site.status === INICIADO || site.status === REINICIANDO"
        />

        <IconeComTooltip
          icone="mdi-stop"
          texto="Parar"
          :acao="() => pararSite(site)"
          :desabilitado="site.status === PARADO || site.status === REINICIANDO"
        />

        <IconeComTooltip
          icone="mdi-restart"
          texto="Reiniciar"
          :acao="() => reiniciarSite(site)"
          :desabilitado="site.status === REINICIANDO"
        />
      </v-card-actions>
    </v-card>
  </ModalPadrao>
</template>

<script setup>
import SiteModel from "@/models/SiteModel";
import IISService from "@/services/IISService";
import { carregandoAsync, notificar } from "@/utils/eventBus";
import { ref, watch } from "vue";

const INICIADO = "Iniciado";
const PARADO = "Parado";
const REINICIANDO = "Reiniciando";

const exibir = defineModel(false);

const sites = ref([]);

watch(exibir, (novoValor) => {
  if (novoValor) consultarSites();
});

const consultarSites = async () => {
  try {
    const resposta = await carregandoAsync(async () => {
      const res = await IISService.getSites();
      return res;
    }, "Consultando sites IIS...");

    sites.value = resposta.map(site => new SiteModel(site));
  } catch (error) {
    console.error("Erro ao consultar sites:", error);
    notificar("erro", "Erro ao consultar sites IIS.");
  }
};

const iniciarSite = async (site) => {
  try {
    await carregandoAsync(async () => {
      await IISService.iniciarSite(site.nome);
    }, `Iniciando site ${site.nome}...`);

    notificar("sucesso", `Site ${site.nome} iniciado com sucesso.`);
    await consultarSites();
  } catch (error) {
    console.error(`Erro ao iniciar site ${site.nome}:`, error);
    notificar("erro", `Erro ao iniciar site ${site.nome}.`);
  }
};

const pararSite = async (site) => {
  try {
    await carregandoAsync(async () => {
      await IISService.pararSite(site.nome);
    }, `Parando site ${site.nome}...`);

    notificar("sucesso", `Site ${site.nome} parado com sucesso.`);
    await consultarSites();
  } catch (error) {
    console.error(`Erro ao parar site ${site.nome}:`, error);
    notificar("erro", `Erro ao parar site ${site.nome}.`);
  }
};

const reiniciarSite = async (site) => {
  try {
    await carregandoAsync(async () => {
      await IISService.reiniciarSite(site.nome);
    }, `Reiniciando site ${site.nome}...`);

    notificar("sucesso", `Site ${site.nome} reiniciado com sucesso.`);
    await consultarSites();
  } catch (error) {
    console.error(`Erro ao reiniciar site ${site.nome}:`, error);
    notificar("erro", `Erro ao reiniciar site ${site.nome}.`);
  }
};


</script>

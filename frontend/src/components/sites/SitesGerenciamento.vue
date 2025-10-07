<template>
  <ModalPadrao
    v-model="exibir"
    titulo="Sites IIS"
    :exibir-botao-primario="false"
    texto-botao-secundario="Fechar"
    icone-botao-secundario="mdi-close"
    :acao-botao-secundario="() => (exibir = false)"
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
        <div>
          <div>
            <v-icon>mdi-gate</v-icon>
            <span class="titulo pl-2">Porta: </span>
            <span>{{ site.porta }}</span>
          </div>

          <div class="pt-2">
            <span class="titulo">status: </span>
            <span>{{ site.status }}</span>
          </div>
        </div>
      </v-card-text>

      <v-card-actions class="d-flex justify-end">
        <IconeComTooltip
          icone="mdi-play"
          :texto="TIPO_COMANDO.INICIAR.titulo"
          :acao="() => iniciarSite(site)"
          :desabilitado="
            site.status === INICIADO || site.status === REINICIANDO
          "
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

<script setup lang="ts">
  import { ref, watch } from 'vue';
  import SiteModel from '@/models/SiteModel';
  import IISService from '@/services/IISService';
  import { carregandoAsync, notificar } from '@/utils/eventBus';
  import type { ISite } from '@/types';
  import { TIPO_COMANDO } from '@/constants/geral-constants';

  const INICIADO = 'Iniciado';
  const PARADO = 'Parado';
  const REINICIANDO = 'Reiniciando';

  const exibir = defineModel<boolean>({ default: false });
  const sites = ref<ISite[]>([]);

  watch(exibir, (novoValor: boolean) => {
    if (novoValor) consultarSites();
  });

  const consultarSites = async (): Promise<void> => {
    try {
      const resposta = await carregandoAsync(async () => {
        const res = await IISService.getSites();
        return res;
      }, 'Consultando sites IIS...');

      sites.value = resposta.map((site: ISite) => new SiteModel(site));
    } catch (error) {
      console.error('Erro ao consultar sites:', error);
      notificar('erro', 'Erro ao consultar sites IIS.');
    }
  };

  const iniciarSite = async (site: ISite): Promise<void> => {
    try {
      await carregandoAsync(async () => {
        await IISService.iniciarSite(site.nome);
      }, `Iniciando site ${site.nome}...`);

      notificar('sucesso', `Site ${site.nome} iniciado.`);
      sites.value = sites.value.map((s: ISite) =>
        s.nome === site.nome ? { ...s, status: INICIADO } : s
      );
    } catch (error) {
      console.error(`Erro ao iniciar site ${site.nome}:`, error);
      notificar('erro', `Erro ao iniciar site ${site.nome}.`);
    }
  };

  const pararSite = async (site: ISite): Promise<void> => {
    try {
      await carregandoAsync(async () => {
        await IISService.pararSite(site.nome);
      }, `Parando site ${site.nome}...`);

      notificar('sucesso', `Site ${site.nome} parado.`);
      sites.value = sites.value.map((s: ISite) =>
        s.nome === site.nome ? { ...s, status: PARADO } : s
      );
    } catch (error) {
      console.error(`Erro ao parar site ${site.nome}:`, error);
      notificar('erro', `Erro ao parar site ${site.nome}.`);
    }
  };

  const reiniciarSite = async (site: ISite): Promise<void> => {
    try {
      await carregandoAsync(async () => {
        await IISService.reiniciarSite(site.nome);
      }, `Reiniciando site ${site.nome}...`);

      notificar('sucesso', `Site ${site.nome} reiniciado.`);
      sites.value = sites.value.map((s: ISite) =>
        s.nome === site.nome ? { ...s, status: REINICIANDO } : s
      );
    } catch (error) {
      console.error(`Erro ao reiniciar site ${site.nome}:`, error);
      notificar('erro', `Erro ao reiniciar site ${site.nome}.`);
    }
  };
</script>

<style scoped>
  .titulo {
    font-weight: bold;
  }
</style>

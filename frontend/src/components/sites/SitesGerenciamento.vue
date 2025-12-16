<template>
  <ModalPadrao
    v-model="exibir"
    titulo="Sites IIS"
    :exibir-botao-primario="false"
    texto-botao-secundario="Fechar"
    icone-botao-secundario="mdi-close"
    :acao-botao-secundario="() => (exibir = false)"
  >
    <draggable
      v-model="sitesOrdenados"
      item-key="nome"
      :animation="200"
      group="sites"
      class="drag-area"
      @end="salvarOrdem"
    >
      <template #item="{ element: site }">
        <v-card class="mb-4 draggable-card">
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
      </template>
    </draggable>
  </ModalPadrao>
</template>

<script setup lang="ts">
  import { ref, watch } from 'vue';
  import SiteModel from '@/models/SiteModel';
  import IISService from '@/services/IISService';
  import { carregandoAsync, notificar } from '@/utils/eventBus';
  import type { ISite } from '@/types';
  import { TIPO_COMANDO } from '@/constants/geral-constants';
  import draggable from 'vuedraggable';

  const INICIADO = 'Iniciado';
  const PARADO = 'Parado';
  const REINICIANDO = 'Reiniciando';
  const CHAVE_ORDEM_SITES = 'OrdemSitesIIS';

  const exibir = defineModel<boolean>({ default: false });
  const sites = ref<ISite[]>([]);
  const sitesOrdenados = ref<ISite[]>([]);

  watch(exibir, (novoValor: boolean) => {
    if (novoValor) consultarSites();
  });

  watch(sites, () => {
    carregarOrdem();
  }, { deep: true });

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

  // Carregar ordem salva do localStorage
  const carregarOrdem = (): void => {
    const ordemSalva = localStorage.getItem(CHAVE_ORDEM_SITES);
    
    if (!ordemSalva || !sites.value.length) {
      sitesOrdenados.value = [...sites.value];
      return;
    }

    try {
      const ordem: string[] = JSON.parse(ordemSalva);
      
      // Ordenar sites baseado na ordem salva
      const sitesOrdenadosTemp: ISite[] = [];
      
      ordem.forEach((nome: string) => {
        const site = sites.value.find((s) => s.nome === nome);
        if (site) {
          sitesOrdenadosTemp.push(site);
        }
      });
      
      // Adicionar sites novos que não estão na ordem salva
      sites.value.forEach((site) => {
        if (!sitesOrdenadosTemp.find((s) => s.nome === site.nome)) {
          sitesOrdenadosTemp.push(site);
        }
      });
      
      sitesOrdenados.value = sitesOrdenadosTemp;
    } catch (error) {
      console.error('Erro ao carregar ordem dos sites:', error);
      sitesOrdenados.value = [...sites.value];
    }
  };

  // Salvar ordem no localStorage
  const salvarOrdem = (): void => {
    const ordem = sitesOrdenados.value.map((site) => site.nome);
    localStorage.setItem(CHAVE_ORDEM_SITES, JSON.stringify(ordem));
  };

  const iniciarSite = async (site: ISite): Promise<void> => {
    try {
      await carregandoAsync(async () => {
        await IISService.iniciarSite(site.nome);
      }, `Iniciando site ${site.nome}...`);

      notificar('sucesso', `Site ${site.nome} iniciado.`);
      sitesOrdenados.value = sitesOrdenados.value.map((s: ISite) =>
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
      sitesOrdenados.value = sitesOrdenados.value.map((s: ISite) =>
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
      sitesOrdenados.value = sitesOrdenados.value.map((s: ISite) =>
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

  .drag-area {
    min-height: 50px;
  }

  .draggable-card {
    cursor: grab;
  }

  .draggable-card:active {
    cursor: grabbing;
  }
</style>

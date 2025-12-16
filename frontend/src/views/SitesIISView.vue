<template>
  <v-container>
    <v-row no-gutters>
      <v-col cols="12">
        <div class="d-flex justify-space-between">
          <div>
            <h1>Sites IIS</h1>
          </div>
        </div>
      </v-col>

      <v-col cols="12">
        <div class="d-flex justify-space-between align-center py-2">
          <BotaoTerciario
            v-if="!emModoCadastroEdicao"
            @click="prepararParaCadastro()"
            texto="Adicionar"
            icone="mdi-plus"
          />
        </div>
      </v-col>

      <v-col cols="12">
        <div>
          <v-tabs-window
            v-model="paginaPrincipal"
            class="altura-limitada"
          >
            <v-tabs-window-item>
              <ListaSitesIIS
                :itens="sites"
                @editar="mudarParaEdicao"
                @excluir="excluirSite"
                @duplicar="duplicarSite"
              />
            </v-tabs-window-item>

            <v-tabs-window-item>
              <v-tabs v-model="paginaCadastro">
                <v-tab>Principal</v-tab>
                <v-tab>Pastas</v-tab>
                <v-tab>Pools</v-tab>
              </v-tabs>

              <v-tabs-window v-model="paginaCadastro">
                <v-tabs-window-item>
                  <SiteIISCadastro
                    v-model="siteSelecionado"
                    class="pt-4"
                  />
                </v-tabs-window-item>
                <v-tabs-window-item>
                  <PastasCadastro v-model="siteSelecionado" />
                </v-tabs-window-item>
                <v-tabs-window-item>
                  <PoolsCadastro
                    v-model="siteSelecionado"
                    class="pt-4"
                  />
                </v-tabs-window-item>
              </v-tabs-window>
            </v-tabs-window-item>
          </v-tabs-window>
        </div>
      </v-col>

      <v-col
        cols="12"
        v-if="!emModoInicial"
      >
        <div class="d-flex align-center justify-end">
          <div>
            <BotaoPrimario
              @click="salvarAlteracoes"
              texto="Salvar"
              icone="mdi-check"
            />
            <BotaoSecundario
              @click="descartarAlteracoes"
              texto="Cancelar"
              icone="mdi-cancel"
            />
          </div>
        </div>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import type { ISiteIIS, ISiteIISDeployResponse } from '@/models/SiteIISModel';
import ListaSitesIIS from '@/components/sitesiis/ListaSitesIIS.vue';
import SiteIISCadastro from '@/components/sitesiis/SiteIISCadastro.vue';
import PastasCadastro from '@/components/sitesiis/PastasCadastro.vue';
import PoolsCadastro from '@/components/sitesiis/PoolsCadastro.vue';
import SiteIISModel from '@/models/SiteIISModel';
import { useSiteIISStore } from '@/stores/siteIIS';
import { carregandoAsync, notificar } from '@/utils/eventBus';
import { MODO_OPERACAO } from '@/constants/geral-constants';

const store = useSiteIISStore();
const sites = reactive<ISiteIISDeployResponse[]>([]);
const siteSelecionado = ref<ISiteIIS>(new SiteIISModel());
const paginaPrincipal = ref<number>(0);
const paginaCadastro = ref<number>(0);

onMounted(async () => {
  await preencherSites();
});

const preencherSites = async (): Promise<void> => {
  try {
    await carregandoAsync(async () => {
      await store.carregarSites();
    });

    // Limpa o array e adiciona os novos itens
    sites.splice(0, sites.length, ...store.sites);
  } catch (error) {
    console.error('Falha ao obter os sites:', error);
  }
};

let modoOperacao = ref<string>(MODO_OPERACAO.INICIAL.valor);

const emModoInicial = computed(
  () => modoOperacao.value === MODO_OPERACAO.INICIAL.valor
);
const emModoCadastro = computed(
  () => modoOperacao.value === MODO_OPERACAO.NOVO.valor
);
const emModoEdicao = computed(
  () => modoOperacao.value === MODO_OPERACAO.EDICAO.valor
);
const emModoCadastroEdicao = computed(
  () => emModoCadastro.value || emModoEdicao.value
);

const irParaListagem = (): void => {
  paginaPrincipal.value = 0;
  modoOperacao.value = MODO_OPERACAO.INICIAL.valor;
};

const irParaCadastro = (): void => {
  paginaCadastro.value = 0;
  paginaPrincipal.value = 1;
};

const mudarParaEdicao = async (identificador: string): Promise<void> => {
  try {
    await store.carregarSite(identificador);
    
    if (!store.siteAtual) {
      notificar('erro', 'Site não encontrado');
      return;
    }

    modoOperacao.value = MODO_OPERACAO.EDICAO.valor;
    Object.assign(siteSelecionado.value, store.siteAtual);
    irParaCadastro();
  } catch (error) {
    console.error('Erro ao carregar site:', error);
    notificar('erro', 'Falha ao carregar site');
  }
};

const prepararParaCadastro = (): void => {
  modoOperacao.value = MODO_OPERACAO.NOVO.valor;
  limparCampos();
  irParaCadastro();
};

const duplicarSite = async (identificador: string): Promise<void> => {
  try {
    await store.carregarSite(identificador);
    
    if (!store.siteAtual) {
      notificar('erro', 'Site não encontrado');
      return;
    }

    // Modo cadastro com dados do site selecionado
    modoOperacao.value = MODO_OPERACAO.NOVO.valor;
    
    // Copia os dados do site mas gera novo identificador
    const siteDuplicado = new SiteIISModel({
      ...store.siteAtual,
      identificador: crypto.randomUUID(), // Novo ID
      nome: `${store.siteAtual.nome} - Cópia`, // Adiciona sufixo
      titulo: `${store.siteAtual.titulo} - Cópia`
    });
    
    Object.assign(siteSelecionado.value, siteDuplicado);
    irParaCadastro();
    notificar('sucesso', 'Site duplicado. Ajuste os dados e salve.');
  } catch (error) {
    console.error('Erro ao duplicar site:', error);
    notificar('erro', 'Falha ao duplicar site');
  }
};

const salvarAlteracoes = async (): Promise<void> => {
  try {
    emModoCadastro.value
      ? await criarSite()
      : await atualizarSite();
  } catch (error) {
    console.error('Falha ao salvar alterações: ', error);
    notificar('erro', 'Falha ao salvar alterações');
  }
};

const criarSite = async (): Promise<void> => {
  try {
    await store.adicionarSite(siteSelecionado.value);
    await preencherSites();
    limparCampos();
    notificar('sucesso', 'Site criado');
    irParaListagem();
    
    // Recarregar lista do dropdown de deploy
    if (typeof (window as any).recarregarSitesParaDeploy === 'function') {
      await (window as any).recarregarSitesParaDeploy();
    }
  } catch (error) {
    console.error('Falha ao criar site: ' + error);
    notificar('erro', 'Falha ao criar site');
  }
};

const atualizarSite = async (): Promise<void> => {
  try {
    await store.atualizarSite(siteSelecionado.value);
    await preencherSites();
    limparCampos();
    notificar('sucesso', 'Site atualizado');
    irParaListagem();
    
    // Recarregar lista do dropdown de deploy
    if (typeof (window as any).recarregarSitesParaDeploy === 'function') {
      await (window as any).recarregarSitesParaDeploy();
    }
  } catch (error) {
    console.error('Falha ao atualizar site' + error);
    notificar('erro', 'Falha ao atualizar site');
  }
};

const excluirSite = async (item: ISiteIISDeployResponse): Promise<void> => {
  const confirmDelete = confirm(
    `Deseja excluir o site "${item.nome}"?`
  );

  if (!confirmDelete) return;

  try {
    await store.excluirSite(item.identificador);
    await preencherSites();
    notificar('sucesso', 'Site excluído');
    
    // Recarregar lista do dropdown de deploy
    if (typeof (window as any).recarregarSitesParaDeploy === 'function') {
      await (window as any).recarregarSitesParaDeploy();
    }
  } catch (error) {
    console.error('Falha ao excluir site' + error);
    notificar('erro', 'Falha ao excluir site');
  }
};

const descartarAlteracoes = (): void => {
  limparCampos();
  irParaListagem();
};

const limparCampos = (): void => {
  Object.assign(siteSelecionado.value, new SiteIISModel());
};
</script>

<style scoped>
.altura-limitada {
  height: calc(100dvh - 220px);
  overflow: auto;
}
</style>

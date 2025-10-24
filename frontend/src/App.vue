<template>
  <v-overlay
    v-model="exibirCarregando"
    class="d-flex flex-column align-center justify-center text-center"
    persistent
    scrim="#121212dd"
    opacity=".9"
  >
    <div
      v-if="exibirConteudoCarregando"
      class="bg-surface rounded-xl px-8 py-6 d-flex flex-column align-center"
    >
      <div class="d-flex align-center mb-3">
        <img
          :src="logo"
          width="24px"
          height="24px"
        />
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

  <SnackbarNotificacao />

  <v-app>
    <v-app-bar>
      <v-app-bar-title>
        <img
          :src="logo"
          width="20px"
        />
        Project Manager Web
      </v-app-bar-title>

      <div>
        <v-btn
          class="text-none"
          @click="exibirModalClone = true"
        >
          <v-icon
            class="pr-2"
            color="primary"
          >
            mdi-git
          </v-icon>
          Clonar
        </v-btn>
        <v-btn
          class="text-none"
          :to="{ name: 'pastas' }"
        >
          <v-icon
            class="pr-2"
            color="primary"
          >
            mdi-folder
          </v-icon>
          Pastas
        </v-btn>
        <v-btn
          class="text-none"
          :to="{ name: 'repositorios' }"
        >
          <v-icon
            class="pr-2"
            color="primary"
          >
            mdi-source-repository
          </v-icon>
          Repositórios
        </v-btn>
        <v-btn
          class="text-none"
          @click="exibirModalSites = true"
        >
          <v-icon
            class="pr-2"
            color="primary"
          >
            mdi-web
          </v-icon>
          Sites IIS
        </v-btn>
        <v-btn
          class="text-none"
          :to="{ name: 'sites-iis' }"
        >
          <v-icon
            class="pr-2"
            color="primary"
          >
            mdi-cog-box
          </v-icon>
          Sites
        </v-btn>
        <v-menu>
          <template v-slot:activator="{ props }">
            <v-btn
              class="text-none"
              v-bind="props"
            >
              <v-icon
                class="pr-2"
                color="success"
              >
                mdi-cloud-upload
              </v-icon>
              Deploy
              <v-icon>mdi-menu-down</v-icon>
            </v-btn>
          </template>
          <v-list>
            <v-list-item
              v-for="site in sitesParaDeploy"
              :key="site.identificador"
              @click="dispararDeploy(site)"
            >
              <v-list-item-title>
                <v-icon
                  size="small"
                  class="mr-2"
                >
                  mdi-rocket-launch
                </v-icon>
                Atualizar {{ site.titulo }}
              </v-list-item-title>
            </v-list-item>
            <v-list-item
              v-if="sitesParaDeploy.length === 0"
              disabled
            >
              <v-list-item-title class="text-grey">
                Nenhum site cadastrado
              </v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>
        
        <v-divider
          vertical
          class="mx-2"
          style="height: 32px; align-self: center;"
        />
        
        <v-btn
          icon
          :to="{ name: 'configuracao' }"
        >
          <v-icon
            class="pr-2"
            color="primary"
          >
            mdi-cog
          </v-icon>
        </v-btn>
      </div>
    </v-app-bar>

    <v-main>
      <router-view />
      
      <div class="watermark-footer">
        Compilado em: {{ compiladoEm }}
      </div>
    </v-main>
  </v-app>

  <CloneGit v-model="exibirModalClone" />
  <SitesGerenciamento v-model="exibirModalSites" />
</template>

<script setup>
  import { onBeforeUnmount, onMounted, ref } from 'vue';
  import logo from '@/assets/logo.svg';
  import VersaoService from './services/VersaoService';
  import SnackbarNotificacao from '@/components/comum/SnackbarNotificacao.vue';
  import CloneGit from '@/components/clone/CloneGit.vue';
  import eventBus, { carregandoAsync } from '@/utils/eventBus';
  import SitesGerenciamento from '@/components/sites/SitesGerenciamento.vue';
  import { UX_CONFIG } from '@/constants/geral-constants';
  import { useSiteIISStore } from '@/stores/siteIIS';

  const compiladoEm = ref();
  const exibirModalClone = ref(false);
  const exibirModalSites = ref(false);

  const siteIISStore = useSiteIISStore();
  const sitesParaDeploy = ref([]);

  const carregarSitesParaDeploy = async () => {
    try {
      await siteIISStore.carregarSites();
      sitesParaDeploy.value = siteIISStore.sites;
    } catch (error) {
      console.error('Erro ao carregar sites:', error);
    }
  };

  // Expor função para recarregar sites (para ser chamada após salvar)
  window.recarregarSitesParaDeploy = carregarSitesParaDeploy;

  const dispararDeploy = async site => {
    try {
      await siteIISStore.dispararDeploy(site.identificador);
      // O acompanhamento será feito via janela do PowerShell
    } catch (error) {
      console.error('Erro ao disparar deploy:', error);
    }
  };

  const consultarVersao = async () => {
    try {
      const response = await carregandoAsync(async () => {
        const res = await VersaoService.obterVersao();
        return res;
      }, 'Consultando a versão...');

      compiladoEm.value = response;
    } catch (error) {
      console.error('Falha ao consultar a versão:', error);
    }
  };

  const exibirCarregando = ref(true);
  const exibirConteudoCarregando = ref(false);
  const mensagem = ref('Carregando...');
  let timeoutDelayCarregando = null;

  const handleCarregando = ({ exibir, texto }) => {
    exibirCarregando.value = exibir;
    mensagem.value = texto;

    if (exibir) {
      // Inicia o delay para mostrar o conteúdo
      timeoutDelayCarregando = setTimeout(() => {
        if (exibirCarregando.value) {
          exibirConteudoCarregando.value = true;
        }
      }, UX_CONFIG.DELAY_LOADING_MS);
    } else {
      // Cancela o timeout se o loading terminar antes do delay
      if (timeoutDelayCarregando) {
        clearTimeout(timeoutDelayCarregando);
        timeoutDelayCarregando = null;
      }
      exibirConteudoCarregando.value = false;
    }
  };

  onMounted(async () => {
    // Configura o listener do evento de carregamento
    eventBus.on('carregando', handleCarregando);

    // Inicia a consulta da versão e aplica o delay inicial
    await consultarVersao();
    await carregarSitesParaDeploy();
  });

  onBeforeUnmount(() => {
    eventBus.off('carregando', handleCarregando);

    // Limpa o timeout se ainda estiver ativo
    if (timeoutDelayCarregando) {
      clearTimeout(timeoutDelayCarregando);
      timeoutDelayCarregando = null;
    }
  });
</script>

<style scoped>
.watermark-footer {
  position: fixed;
  bottom: 8px;
  left: 16px;
  font-size: 11px;
  color: rgba(255, 255, 255, 0.15);
  pointer-events: none;
  user-select: none;
  font-weight: 300;
  letter-spacing: 0.5px;
}
</style>

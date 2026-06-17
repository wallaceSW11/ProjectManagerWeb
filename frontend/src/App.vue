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
          :to="{ name: 'ides' }"
        >
          <v-icon
            class="pr-2"
            color="primary"
          >
            mdi-application-cog
          </v-icon>
          IDEs
        </v-btn>
        <v-btn
          v-if="featuresStore.iis"
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
        <v-menu v-if="featuresStore.deploy">
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
            <v-list-item :to="{ name: 'sites-iis' }">
              <v-list-item-title>
                <v-icon
                  size="small"
                  class="mr-2"
                  color="primary"
                >
                  mdi-cog-box
                </v-icon>
                Gerenciar sites
              </v-list-item-title>
            </v-list-item>
            <v-divider
              v-if="sitesParaDeploy.length > 0"
              class="my-1"
            />
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
          style="height: 32px; align-self: center"
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

        <v-menu close-on-content-click="false">
          <template v-slot:activator="{ props }">
            <v-btn
              icon
              v-bind="props"
              :loading="versaoStore.carregando"
            >
              <v-badge
                v-if="versaoStore.temAtualizacao"
                dot
                color="error"
              >
                <v-icon color="primary">mdi-download</v-icon>
              </v-badge>
              <v-icon
                v-else
                color="primary"
              >
                mdi-download
              </v-icon>
            </v-btn>
          </template>

          <v-list density="compact">
            <v-list-item v-if="versaoStore.versaoAtual">
              <v-list-item-title class="text-body-2">
                Versão atual:
                <strong>{{ versaoStore.versaoAtual }}</strong>
              </v-list-item-title>
            </v-list-item>

            <v-list-item v-if="versaoStore.carregando">
              <v-list-item-title class="text-body-2 text-grey">
                <v-icon
                  size="small"
                  class="mr-1"
                >
                  mdi-loading mdi-spin
                </v-icon>
                Verificando...
              </v-list-item-title>
            </v-list-item>

            <v-list-item v-if="versaoStore.temAtualizacao">
              <v-list-item-title
                class="text-body-2 text-success font-weight-bold"
              >
                <v-icon
                  size="small"
                  class="mr-1"
                  color="success"
                >
                  mdi-alert
                </v-icon>
                Nova versão: {{ versaoStore.versaoNova }}
              </v-list-item-title>
            </v-list-item>

            <v-divider class="my-1" />

            <v-list-item
              v-if="versaoStore.temAtualizacao"
              @click="atualizarAgora"
            >
              <v-list-item-title class="text-body-2">
                <v-icon
                  size="small"
                  class="mr-1"
                  color="success"
                >
                  mdi-update
                </v-icon>
                Atualizar agora
              </v-list-item-title>
            </v-list-item>

            <v-list-item @click="verificarAtualizacaoManual">
              <v-list-item-title class="text-body-2">
                <v-icon
                  size="small"
                  class="mr-1"
                >
                  mdi-refresh
                </v-icon>
                Verificar atualizações
              </v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>
      </div>
    </v-app-bar>

    <v-main>
      <router-view />
    </v-main>
  </v-app>

  <CloneGit v-model="exibirModalClone" />
  <SitesGerenciamento v-model="exibirModalSites" />
</template>

<script setup>
  import { onBeforeUnmount, onMounted, ref } from 'vue';
  import logo from '@/assets/logo.svg';
  import ComandosService from '@/services/ComandosService';
  import SnackbarNotificacao from '@/components/comum/SnackbarNotificacao.vue';
  import CloneGit from '@/components/clone/CloneGit.vue';
  import eventBus, { notificar } from '@/utils/eventBus';
  import SitesGerenciamento from '@/components/sites/SitesGerenciamento.vue';
  import { UX_CONFIG } from '@/constants/geral-constants';
  import { useSiteIISStore } from '@/stores/siteIIS';
  import { useFeaturesStore } from '@/stores/features';
  import { useVersaoStore } from '@/stores/useVersaoStore';

  const exibirModalClone = ref(false);
  const exibirModalSites = ref(false);

  const featuresStore = useFeaturesStore();
  const siteIISStore = useSiteIISStore();
  const versaoStore = useVersaoStore();
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

  const atualizarAgora = async () => {
    notificar(
      'sucesso',
      'Atualização iniciada',
      'Acompanhe o progresso no terminal.'
    );
    ComandosService.executarComandoAvulso({ comando: 'pmw update' });

    notificar('aviso', 'Aguardando servidor reiniciar...');

    const aguardarReinicio = () => {
      return new Promise(resolve => {
        const maxTentativas = 60;
        const intervalo = 2000;
        let servidorFicouOffline = false;
        let tentativas = 0;

        const verificar = async () => {
          tentativas++;
          try {
            const resposta = await fetch('/api/versao');
            if (resposta.ok && servidorFicouOffline) {
              resolve();
              return;
            }
          } catch {
            servidorFicouOffline = true;
          }

          if (tentativas >= maxTentativas) {
            resolve();
            return;
          }

          setTimeout(verificar, intervalo);
        };

        setTimeout(verificar, intervalo);
      });
    };

    await aguardarReinicio();
    window.location.reload();
  };

  const verificarAtualizacaoManual = async () => {
    await versaoStore.verificarAtualizacao();
    if (versaoStore.erro)
      notificar('erro', 'Falha ao verificar atualizações', versaoStore.erro);
    else if (!versaoStore.temAtualizacao)
      notificar('sucesso', 'Você está na versão mais recente');
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

    await featuresStore.carregar();
    versaoStore.carregar();

    if (featuresStore.iis) {
      await carregarSitesParaDeploy();
    }
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

<style>
  .v-main {
    overflow: hidden;
  }
</style>

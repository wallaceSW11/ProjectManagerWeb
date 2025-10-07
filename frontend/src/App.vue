<template>
  <v-overlay
    v-model="exibirCarregando"
    class="d-flex flex-column align-center justify-center text-center"
    persistent
    scrim="#121212dd"
    opacity=".9"
  >
    <div
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
            >mdi-git</v-icon
          >
          Clonar
        </v-btn>
        <v-btn
          class="text-none"
          :to="{ name: 'pastas' }"
        >
          <v-icon
            class="pr-2"
            color="primary"
            >mdi-folder</v-icon
          >
          Pastas
        </v-btn>
        <v-btn
          class="text-none"
          :to="{ name: 'repositorios' }"
        >
          <v-icon
            class="pr-2"
            color="primary"
            >mdi-source-repository</v-icon
          >
          Repositórios
        </v-btn>
        <v-btn
          class="text-none"
          @click="exibirModalSites = true"
        >
          <v-icon
            class="pr-2"
            color="primary"
            >mdi-web</v-icon
          >
          Sites IIS
        </v-btn>
        <v-btn
          icon
          :to="{ name: 'configuracao' }"
        >
          <v-icon
            class="pr-2"
            color="primary"
            >mdi-cog</v-icon
          >
        </v-btn>

        <v-btn>
          <v-icon color="primary">mdi-calendar</v-icon>
          <v-tooltip
            activator="parent"
            location="left"
            >{{ `Compilado em: ${compiladoEm}` }}</v-tooltip
          >
        </v-btn>
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
  import { onBeforeUnmount, onMounted, ref } from 'vue'
  import logo from '@/assets/logo.svg'
  import VersaoService from './services/VersaoService'
  import SnackbarNotificacao from '@/components/comum/SnackbarNotificacao.vue'
  import CloneGit from '@/components/clone/CloneGit.vue'
  import eventBus, { carregandoAsync } from '@/utils/eventBus'
  import SitesGerenciamento from '@/components/sites/SitesGerenciamento.vue'

  const compiladoEm = ref()
  const exibirModalClone = ref(false)
  const exibirModalSites = ref(false)

  onMounted(async () => {
    await consultarVersao()
  })

  const consultarVersao = async () => {
    try {
      const response = await carregandoAsync(async () => {
        const res = await VersaoService.obterVersao()

        return res
      }, 'Consultando a versão...')

      compiladoEm.value = response
    } catch (error) {
      console.error('Falha ao consultar a versão:', error)
    }
  }

  const exibirCarregando = ref(true)
  const mensagem = ref('Carregando...')

  const handleCarregando = ({ exibir, texto }) => {
    exibirCarregando.value = exibir
    mensagem.value = texto
  }

  onMounted(() => {
    eventBus.on('carregando', handleCarregando)
  })

  onBeforeUnmount(() => {
    eventBus.off('carregando', handleCarregando)
  })
</script>

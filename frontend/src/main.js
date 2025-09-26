import { createApp } from 'vue'
import { createPinia } from 'pinia'
import './style.css'
import App from './App.vue'
import router from './router'
import { vuetify } from './plugins/vuetify'
import { useConfiguracaoStore } from './stores/configuracao'

import BotaoPrimario from './components/comum/botao/BotaoPrimario.vue'
import BotaoSecundario from './components/comum/botao/BotaoSecundario.vue'
import ModalPadrao from './components/comum/ModalPadrao.vue'

async function initApp() {
  const app = createApp(App)
  const pinia = createPinia()

  app.use(pinia)
  app.use(router)
  app.use(vuetify)

  app.component('BotaoPrimario', BotaoPrimario)
  app.component('BotaoSecundario', BotaoSecundario)
  app.component('ModalPadrao', ModalPadrao)

  // Carregar configuração antes de montar a aplicação
  const configuracaoStore = useConfiguracaoStore()
  await configuracaoStore.carregarConfiguracao()

  app.mount('#app')
}

initApp()

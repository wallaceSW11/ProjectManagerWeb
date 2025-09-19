import { createApp } from 'vue'
import { createPinia } from 'pinia'
import './style.css'
import App from './App.vue'
import router from './router'
import { vuetify } from './plugins/vuetify'
import { useConfiguracaoStore } from './stores/configuracao'

async function initApp() {
  const app = createApp(App)
  const pinia = createPinia()

  app.use(pinia)
  app.use(router)
  app.use(vuetify)

  // Carregar configuração antes de montar a aplicação
  const configuracaoStore = useConfiguracaoStore()
  await configuracaoStore.carregarConfiguracao()

  app.mount('#app')
}

initApp()

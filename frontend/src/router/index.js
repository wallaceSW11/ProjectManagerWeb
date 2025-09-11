import { createRouter, createWebHistory } from 'vue-router'
import PastasView from '../views/PastasView.vue'
import ConfiguracaoView from '../views/ConfiguracaoView.vue'
import RepositoriosView from '../views/RepositoriosView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'pastas',
      component: PastasView
    },
    {
      path: '/configuracao',
      name: 'configuracao',
      component: ConfiguracaoView
    },
    {
      path: '/repositorios',
      name: 'repositorios',
      component: RepositoriosView
    }
  ]
})

export default router
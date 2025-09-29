import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'pastas',
      component: () => import('../views/PastasView.vue')
    },
    {
      path: '/configuracao',
      name: 'configuracao',
      component: () => import('../views/ConfiguracaoView.vue')
    },
    {
      path: '/repositorios',
      name: 'repositorios',
      component: () => import('../views/RepositoriosView.vue')
    }
  ]
})

export default router
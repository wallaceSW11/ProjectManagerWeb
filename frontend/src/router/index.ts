import {
  createRouter,
  createWebHistory,
  type RouteRecordRaw,
} from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'pastas',
    component: () => import('../views/PastasView.vue'),
  },
  {
    path: '/configuracao',
    name: 'configuracao',
    component: () => import('../views/ConfiguracaoView.vue'),
  },
  {
    path: '/repositorios',
    name: 'repositorios',
    component: () => import('../views/RepositoriosView.vue'),
  },
  {
    path: '/sites-iis',
    name: 'sites-iis',
    component: () => import('../views/SitesIISView.vue'),
  },
  {
    path: '/ides',
    name: 'ides',
    component: () => import('../views/IDEsView.vue'),
  },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

export default router;

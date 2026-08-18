<template>
  <div
    class="monitoramento"
    :class="{ 'monitoramento-cockpit': ehPainelEsportivo }"
  >
    <header
      class="monitoramento-barra"
      :class="{ 'monitoramento-barra-cockpit': ehPainelEsportivo }"
    >
      <img
        v-if="!ehPainelEsportivo"
        :src="logo"
        class="monitoramento-logo"
      />
      <v-icon
        v-else
        color="#ffd21c"
        size="16"
      >
        mdi-flash
      </v-icon>
      <span class="monitoramento-titulo">{{ titulo }}</span>

      <div class="monitoramento-acoes">
        <v-menu>
          <template #activator="{ props: menuProps }">
            <v-btn
              v-bind="menuProps"
              class="monitoramento-seletor-layout"
              variant="plain"
              density="comfortable"
              icon="mdi-view-dashboard-outline"
              aria-label="Selecionar layout"
            />
          </template>

          <v-list
            density="compact"
            class="monitoramento-menu-layout"
          >
            <v-list-item
              v-for="opcao in opcoesLayout"
              :key="opcao.valor"
              :title="opcao.titulo"
              :active="layoutAtual === opcao.valor"
              @click="selecionarLayout(opcao.valor)"
            />
          </v-list>
        </v-menu>
      </div>
    </header>

    <main class="monitoramento-corpo">
      <component :is="layoutComponente" />
    </main>
  </div>
</template>

<script setup lang="ts">
  import { computed, onBeforeUnmount, onMounted } from 'vue';
  import logo from '@/assets/logo.svg';
  import LayoutPainelEsportivo from '@/components/monitoramento/layouts/LayoutPainelEsportivo.vue';
  import LayoutPadrao from '@/components/monitoramento/layouts/LayoutPadrao.vue';
  import { LAYOUT_MONITORAMENTO } from '@/constants/geral-constants';
  import { useLayoutMonitoramento } from '@/composables/useLayoutMonitoramento';
  import { useMonitoramentoStore } from '@/stores/monitoramento';

  const monitoramentoStore = useMonitoramentoStore();

  const { layoutAtual, ehPainelEsportivo, selecionarLayout } =
    useLayoutMonitoramento();

  const opcoesLayout = [
    LAYOUT_MONITORAMENTO.PADRAO,
    LAYOUT_MONITORAMENTO.PAINEL_ESPORTIVO
  ];

  const layoutComponente = computed(() =>
    ehPainelEsportivo.value ? LayoutPainelEsportivo : LayoutPadrao
  );

  const titulo = computed(() =>
    ehPainelEsportivo.value ? 'PMW MONITOR' : 'Project Manager Web Monitoring'
  );

  onMounted(() => {
    monitoramentoStore.conectar();
  });

  onBeforeUnmount(() => {
    monitoramentoStore.desconectar();
  });
</script>

<style scoped>
  .monitoramento {
    height: 100dvh;
    display: flex;
    flex-direction: column;
    background: rgb(var(--v-theme-background));
  }

  .monitoramento-cockpit {
    background: #07090a;
  }

  .monitoramento-barra {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 8px 16px;
    border-bottom: 1px solid rgba(var(--v-theme-on-surface), 0.08);
    flex-shrink: 0;
  }

  .monitoramento-barra-cockpit {
    background: rgba(5, 7, 8, 0.72);
    border-bottom-color: rgba(255, 255, 255, 0.1);
    backdrop-filter: blur(8px);
  }

  .monitoramento-logo {
    width: 28px;
    height: 28px;
  }

  .monitoramento-titulo {
    font-size: 1.1rem;
    font-weight: 600;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .monitoramento-barra-cockpit .monitoramento-titulo {
    color: #f3f4f4;
    font-size: 0.9rem;
    letter-spacing: 0.04em;
  }

  .monitoramento-acoes {
    margin-left: auto;
    display: flex;
    align-items: center;
    gap: 16px;
  }

  .monitoramento-seletor-layout {
    letter-spacing: 0.03em;
  }

  .monitoramento-barra-cockpit .monitoramento-seletor-layout {
    color: #f3f4f4;
  }

  .monitoramento-corpo {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-height: 0;
    min-width: 0;
  }
</style>

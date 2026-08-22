<template>
  <div class="monitoramento">
    <header class="monitoramento-barra">
      <v-icon
        color="#ffd21c"
        size="14"
      >
        mdi-flash
      </v-icon>
      <span class="monitoramento-titulo">PMW MONITOR</span>
    </header>

    <main class="monitoramento-corpo">
      <LayoutPainelEsportivo />
    </main>
  </div>
</template>

<script setup lang="ts">
  import { onBeforeUnmount, onMounted } from 'vue';
  import LayoutPainelEsportivo from '@/components/monitoramento/layouts/LayoutPainelEsportivo.vue';
  import { useMonitoramentoStore } from '@/stores/monitoramento';

  const monitoramentoStore = useMonitoramentoStore();

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
    background: #07090a;
  }

  .monitoramento-barra {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 3px 14px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    background: rgba(5, 7, 8, 0.72);
    backdrop-filter: blur(8px);
    flex-shrink: 0;
  }

  .monitoramento-titulo {
    color: #747c7e;
    font-size: 0.68rem;
    font-weight: 700;
    letter-spacing: 0.1em;
    white-space: nowrap;
  }

  .monitoramento-corpo {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-height: 0;
    min-width: 0;
  }
</style>

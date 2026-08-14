<template>
  <div class="cockpit">
    <header class="cockpit-barra">
      <img
        :src="logo"
        class="cockpit-logo"
      />
      <span class="cockpit-titulo">Project Manager Web Monitoring</span>
      <v-btn
        class="cockpit-home"
        icon
        variant="text"
        :to="{ name: 'pastas' }"
      >
        <v-icon color="primary">mdi-home</v-icon>
      </v-btn>
    </header>

    <main class="cockpit-corpo">
      <CardCircular
        icone="mdi-chip"
        titulo="CPU"
        :nome="cpuNome"
        :percentual="cpuPercentual"
        rotulo-esquerdo="Frequência"
        :valor-esquerdo="cpuFrequenciaTexto"
        rotulo-direito="Temperatura"
        :valor-direito="cpuTemperaturaTexto"
      />

      <CardCircular
        icone="mdi-memory"
        titulo="RAM"
        :nome="null"
        :percentual="ramPercentual"
        rotulo-esquerdo="Em uso"
        :valor-esquerdo="ramUsadaTexto"
        rotulo-direito="Velocidade"
        :valor-direito="ramVelocidadeTexto"
      />

      <CardMetrica
        icone="mdi-harddisk"
        titulo="Disco"
        :valor="discoValor"
        :detalhe="discoDetalhe"
        :percentual="discoPercentual"
        :cor="corDisco"
      />

      <section class="cockpit-card cockpit-sistema">
        <div class="d-flex align-center ga-2 mb-2">
          <v-icon
            :color="corConexao"
            size="small"
          >
            {{ iconeConexao }}
          </v-icon>
          <span class="text-caption text-grey text-uppercase">Sistema</span>
        </div>

        <div class="cockpit-so">
          {{ sistemaOperacional }}
        </div>

        <div class="text-caption text-grey">
          {{ textoConexao }}
        </div>
      </section>
    </main>
  </div>
</template>

<script setup lang="ts">
  import { computed, onBeforeUnmount, onMounted } from 'vue';
  import logo from '@/assets/logo.svg';
  import CardCircular from '@/components/monitoramento/CardCircular.vue';
  import CardMetrica from '@/components/monitoramento/CardMetrica.vue';
  import { useMonitoramentoStore } from '@/stores/monitoramento';
  import { corPorUso } from '@/utils/corUso';

  const monitoramentoStore = useMonitoramentoStore();

  const formatarGb = (bytes: number): string => (bytes / 1024 ** 3).toFixed(1);

  const cpuNome = computed(() => monitoramentoStore.snapshot?.cpuNome || '--');

  const cpuPercentual = computed(
    () => monitoramentoStore.snapshot?.cpuPercentual ?? null
  );

  const cpuFrequenciaTexto = computed(() => {
    const mhz = monitoramentoStore.snapshot?.cpuFrequenciaMhz ?? null;
    if (mhz === null) return '--';
    return mhz >= 1000
      ? `${(mhz / 1000).toFixed(1)} GHz`
      : `${Math.round(mhz)} MHz`;
  });

  const cpuTemperaturaTexto = computed(() => {
    const celsius = monitoramentoStore.snapshot?.cpuTemperaturaCelsius ?? null;
    return celsius === null ? '--' : `${Math.round(celsius)}°C`;
  });

  const ramUsadaBytes = computed(
    () => monitoramentoStore.snapshot?.ramUsadaBytes ?? null
  );

  const ramTotalBytes = computed(
    () => monitoramentoStore.snapshot?.ramTotalBytes ?? null
  );

  const ramPercentual = computed(() => {
    const usada = ramUsadaBytes.value;
    const total = ramTotalBytes.value;
    if (usada === null || total === null || total === 0) return null;
    return (usada / total) * 100;
  });

  const ramUsadaTexto = computed(() => {
    const usada = ramUsadaBytes.value;
    return usada === null ? '--' : `${formatarGb(usada)} GB`;
  });

  const ramVelocidadeTexto = computed(() => {
    const mhz = monitoramentoStore.snapshot?.ramVelocidadeMhz ?? null;
    return mhz === null ? '--' : `${Math.round(mhz)} MHz`;
  });

  const discoUsadaBytes = computed(
    () => monitoramentoStore.snapshot?.discoUsadaBytes ?? null
  );

  const discoTotalBytes = computed(
    () => monitoramentoStore.snapshot?.discoTotalBytes ?? null
  );

  const discoPercentual = computed(() => {
    const valor = monitoramentoStore.snapshot?.discoPercentual;
    return valor === null || valor === undefined ? null : valor;
  });

  const discoValor = computed(() => {
    const percentual = discoPercentual.value;
    return percentual === null ? '--' : `${percentual.toFixed(1)}%`;
  });

  const discoDetalhe = computed(() => {
    const usada = discoUsadaBytes.value;
    const total = discoTotalBytes.value;
    if (usada === null || total === null || total === 0) return 'aguardando...';
    return `${formatarGb(usada)} de ${formatarGb(total)} GB`;
  });

  const corDisco = computed(() =>
    discoPercentual.value === null
      ? 'primary'
      : corPorUso(discoPercentual.value)
  );

  const sistemaOperacional = computed(
    () => monitoramentoStore.snapshot?.sistemaOperacional || '--'
  );

  const textoConexao = computed(() =>
    monitoramentoStore.conectado ? 'Conectado' : 'Desconectado'
  );

  const iconeConexao = computed(() =>
    monitoramentoStore.conectado ? 'mdi-lan-connect' : 'mdi-lan-disconnect'
  );

  const corConexao = computed(() =>
    monitoramentoStore.conectado ? 'success' : 'error'
  );

  onMounted(() => {
    monitoramentoStore.conectar();
  });

  onBeforeUnmount(() => {
    monitoramentoStore.desconectar();
  });
</script>

<style scoped>
  .cockpit {
    height: 100dvh;
    display: flex;
    flex-direction: column;
    background: rgb(var(--v-theme-background));
  }

  .cockpit-barra {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 8px 16px;
    border-bottom: 1px solid rgba(var(--v-theme-on-surface), 0.08);
    flex-shrink: 0;
  }

  .cockpit-logo {
    width: 28px;
    height: 28px;
  }

  .cockpit-titulo {
    font-size: 1.1rem;
    font-weight: 600;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .cockpit-home {
    margin-left: auto;
  }

  .cockpit-corpo {
    flex: 1;
    display: flex;
    gap: 12px;
    padding: 12px;
    min-height: 0;
  }

  .cockpit-corpo > * {
    flex: 1;
  }

  .cockpit-so {
    font-size: 1.2rem;
    font-weight: 700;
    line-height: 1.3;
    margin-bottom: 4px;
  }

  @media (max-width: 600px) {
    .cockpit-corpo {
      flex-direction: column;
    }
  }
</style>

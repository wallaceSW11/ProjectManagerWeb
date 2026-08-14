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
      <CardMetrica
        icone="mdi-chip"
        titulo="CPU"
        :valor="cpuValor"
        :detalhe="cpuDetalhe"
        :percentual="cpuPercentual"
        :cor="corCpu"
      />

      <CardMetrica
        icone="mdi-memory"
        titulo="RAM"
        :valor="ramValor"
        :detalhe="ramDetalhe"
        :percentual="ramPercentual"
        :cor="corRam"
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

        <div class="text-caption text-grey">
          {{ clientesConectados }}
          {{ rotuloClientes }} · atualizado {{ timestampFormatado }}
        </div>
      </section>
    </main>
  </div>
</template>

<script setup lang="ts">
  import { computed, onBeforeUnmount, onMounted } from 'vue';
  import logo from '@/assets/logo.svg';
  import CardMetrica from '@/components/monitoramento/CardMetrica.vue';
  import { useMonitoramentoStore } from '@/stores/monitoramento';

  const monitoramentoStore = useMonitoramentoStore();

  const formatarGb = (bytes: number): string => (bytes / 1024 ** 3).toFixed(1);

  const corPorUso = (percentual: number): string =>
    percentual < 60 ? 'success' : percentual < 85 ? 'warning' : 'error';

  const cpuPercentual = computed(() => {
    const valor = monitoramentoStore.snapshot?.cpuPercentual;
    return valor === null || valor === undefined ? null : valor;
  });

  const cpuValor = computed(() => {
    const percentual = cpuPercentual.value;
    return percentual === null ? '--' : `${percentual.toFixed(1)}%`;
  });

  const cpuDetalhe = computed(() =>
    cpuPercentual.value === null ? 'aguardando amostra...' : 'uso atual'
  );

  const corCpu = computed(() =>
    cpuPercentual.value === null ? 'primary' : corPorUso(cpuPercentual.value)
  );

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

  const ramValor = computed(() => {
    const usada = ramUsadaBytes.value;
    return usada === null ? '--' : `${formatarGb(usada)} GB`;
  });

  const ramDetalhe = computed(() => {
    const usada = ramUsadaBytes.value;
    const total = ramTotalBytes.value;
    if (usada === null || total === null || total === 0) return 'aguardando...';
    return `${formatarGb(usada)} de ${formatarGb(total)} GB`;
  });

  const corRam = computed(() =>
    ramPercentual.value === null ? 'primary' : corPorUso(ramPercentual.value)
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

  const clientesConectados = computed(
    () => monitoramentoStore.clientesConectados
  );

  const rotuloClientes = computed(() =>
    clientesConectados.value === 1 ? 'cliente' : 'clientes'
  );

  const timestampFormatado = computed(() => {
    const timestamp = monitoramentoStore.snapshot?.timestamp;
    if (!timestamp) return '--';
    return new Date(timestamp).toLocaleTimeString('pt-BR');
  });

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

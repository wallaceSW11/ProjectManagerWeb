<template>
  <div class="cockpit-dashboard">
    <div class="cockpit-gauges">
      <SportGauge
        :titulo="cpuTitulo"
        :valor="cpuPercentual"
        cor="#74d94b"
        :secundario="cpuSecundario"
      />

      <SportGauge
        titulo="RAM"
        :valor="ramPercentual"
        cor="#ff9f12"
        :secundario="ramSecundario"
      />
    </div>

    <footer class="cockpit-so">{{ sistemaOperacional }}</footer>
  </div>
</template>

<script setup lang="ts">
  import { computed } from 'vue';
  import SportGauge from '@/components/monitoramento/painel/SportGauge.vue';
  import { useMonitoramentoStore } from '@/stores/monitoramento';

  const monitoramentoStore = useMonitoramentoStore();

  const formatarGb = (bytes: number): string => (bytes / 1024 ** 3).toFixed(1);

  const cpuTitulo = computed(
    () => monitoramentoStore.snapshot?.cpuNome || '--'
  );

  const cpuPercentual = computed(
    () => monitoramentoStore.snapshot?.cpuPercentual ?? null
  );

  const cpuSecundario = computed(() => {
    const snapshot = monitoramentoStore.snapshot;
    const mhz = snapshot?.cpuFrequenciaMhz ?? null;
    const celsius = snapshot?.cpuTemperaturaCelsius ?? null;
    const frequencia =
      mhz === null
        ? '--'
        : mhz >= 1000
          ? `${(mhz / 1000).toFixed(2)} GHz`
          : `${Math.round(mhz)} MHz`;
    const temperatura = celsius === null ? '--' : `${Math.round(celsius)}°C`;
    return `${frequencia} · ${temperatura}`;
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

  const ramSecundario = computed(() => {
    const usada = ramUsadaBytes.value;
    const total = ramTotalBytes.value;
    if (usada === null || total === null) return '-- · --';
    return `${formatarGb(usada)} GB · ${formatarGb(total)} GB`;
  });

  const sistemaOperacional = computed(
    () => monitoramentoStore.snapshot?.sistemaOperacional || '--'
  );
</script>

<style scoped>
  .cockpit-dashboard {
    flex: 1;
    display: flex;
    flex-direction: column;
    position: relative;
    min-height: 0;
    overflow: hidden;
    background:
      radial-gradient(
        circle at 50% 50%,
        rgba(255, 255, 255, 0.025),
        transparent 45%
      ),
      linear-gradient(180deg, #0d1011 0%, #07090a 100%);
    color: #f3f4f4;
  }

  .cockpit-dashboard::before {
    content: '';
    position: absolute;
    inset: 0;
    opacity: 0.08;
    pointer-events: none;
    background:
      repeating-linear-gradient(
        135deg,
        rgba(255, 255, 255, 0.08) 0,
        rgba(255, 255, 255, 0.08) 1px,
        transparent 1px,
        transparent 7px
      ),
      repeating-linear-gradient(
        45deg,
        rgba(255, 255, 255, 0.04) 0,
        rgba(255, 255, 255, 0.04) 1px,
        transparent 1px,
        transparent 7px
      );
    mask-image: linear-gradient(
      to bottom,
      transparent,
      black 20%,
      black 80%,
      transparent
    );
  }

  .cockpit-gauges {
    flex: 1;
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
    gap: clamp(10px, 2vw, 28px);
    align-items: center;
    justify-items: center;
    padding: clamp(8px, 2vh, 20px) clamp(10px, 3vw, 40px);
    min-height: 0;
  }

  .cockpit-so {
    position: absolute;
    z-index: 4;
    bottom: 10px;
    left: 50%;
    transform: translateX(-50%);
    color: #899092;
    font-size: clamp(10px, 1.3vw, 14px);
    letter-spacing: 0.05em;
    white-space: nowrap;
    pointer-events: none;
  }

  @media (orientation: portrait) {
    .cockpit-gauges {
      grid-template-columns: 1fr;
      grid-template-rows: 1fr 1fr;
      overflow-y: auto;
    }
  }
</style>

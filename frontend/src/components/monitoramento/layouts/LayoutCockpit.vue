<template>
  <div class="cockpit-dashboard">
    <div class="cockpit-gauges">
      <SportGauge
        :titulo="cpuTitulo"
        :valor="cpuPercentual"
        cor="#74d94b"
      >
        <template #secundario>
          <div class="cockpit-secundario">
            <span class="cockpit-secundario-item">
              <v-icon
                size="18"
                color="#74d94b"
              >
                mdi-speedometer
              </v-icon>
              {{ cpuFrequenciaTexto }}
            </span>

            <span class="cockpit-secundario-item">
              <v-icon
                size="18"
                color="#ff9f12"
              >
                mdi-thermometer
              </v-icon>
              {{ cpuTemperaturaTexto }}
            </span>
          </div>
        </template>
      </SportGauge>

      <SportGauge
        titulo="RAM"
        :valor="ramPercentual"
        cor="#ff9f12"
        :secundario="ramSecundario"
      />
    </div>

    <footer class="cockpit-rodape">
      <div class="cockpit-so">{{ sistemaOperacional }}</div>

      <div class="cockpit-disco">
        <v-icon
          size="16"
          color="#899092"
        >
          mdi-harddisk
        </v-icon>
        {{ discoTexto }}
      </div>
    </footer>
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

  const cpuFrequenciaTexto = computed(() => {
    const mhz = monitoramentoStore.snapshot?.cpuFrequenciaMhz ?? null;
    if (mhz === null) return '--';
    return mhz >= 1000
      ? `${(mhz / 1000).toFixed(2)} GHz`
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

  const ramSecundario = computed(() => {
    const usada = ramUsadaBytes.value;
    const total = ramTotalBytes.value;
    if (usada === null || total === null) return '-- · --';
    return `${formatarGb(usada)} GB · ${formatarGb(total)} GB`;
  });

  const sistemaOperacional = computed(
    () => monitoramentoStore.snapshot?.sistemaOperacional || '--'
  );

  const discoPercentual = computed(
    () => monitoramentoStore.snapshot?.discoPercentual ?? null
  );

  const discoUsadaBytes = computed(
    () => monitoramentoStore.snapshot?.discoUsadaBytes ?? null
  );

  const discoTotalBytes = computed(
    () => monitoramentoStore.snapshot?.discoTotalBytes ?? null
  );

  const discoTexto = computed(() => {
    const percentual = discoPercentual.value;
    const usada = discoUsadaBytes.value;
    const total = discoTotalBytes.value;
    const percentualTexto =
      percentual === null ? '--' : `${percentual.toFixed(1)}%`;
    const usadaTexto = usada === null ? '--' : `${formatarGb(usada)} GB`;
    const totalTexto = total === null ? '--' : `${formatarGb(total)} GB`;
    return `${percentualTexto} · ${usadaTexto} de ${totalTexto}`;
  });
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
    container-type: size;
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
    gap: clamp(10px, 2vw, 28px);
    align-items: center;
    justify-items: center;
    padding: clamp(8px, 2vh, 20px) clamp(10px, 3vw, 40px)
      clamp(12px, 2.5vh, 22px);
    min-height: 0;
  }

  .cockpit-rodape {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 4px;
    flex-shrink: 0;
    padding: 10px 10px 18px;
    white-space: nowrap;
    pointer-events: none;
  }

  .cockpit-so {
    color: #899092;
    font-size: clamp(12px, 1.5vw, 16px);
    letter-spacing: 0.05em;
  }

  .cockpit-disco {
    display: flex;
    align-items: center;
    gap: 6px;
    color: #6b7375;
    font-size: clamp(11px, 1.3vw, 14px);
    font-variant-numeric: tabular-nums;
    letter-spacing: 0.04em;
  }

  .cockpit-secundario {
    display: flex;
    align-items: center;
    gap: 14px;
  }

  .cockpit-secundario-item {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    white-space: nowrap;
  }

  @media (orientation: portrait) {
    .cockpit-gauges {
      grid-template-columns: 1fr;
      grid-template-rows: 1fr 1fr;
      overflow-y: auto;
    }

    .cockpit-rodape {
      padding: 16px 10px 24px;
    }
  }
</style>

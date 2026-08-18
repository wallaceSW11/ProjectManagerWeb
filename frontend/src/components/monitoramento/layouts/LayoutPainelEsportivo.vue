<template>
  <section class="painel-esportivo">
    <article class="painel-esportivo-instrumento">
      <div class="painel-esportivo-cpu-nome">{{ cpuNome }}</div>

      <ContaGiros
        titulo="CPU"
        :valor="cpuPercentual"
        cor="#74d94b"
        :detalhes="detalhesCpu"
      />
    </article>

    <aside class="painel-esportivo-centro">
      <span class="painel-esportivo-centro-rotulo">SISTEMA</span>
      <strong class="painel-esportivo-sistema">{{ sistemaOperacional }}</strong>
      <div class="painel-esportivo-divisor" />
      <span class="painel-esportivo-centro-rotulo">DISCO</span>
      <strong class="painel-esportivo-disco-percentual">
        {{ discoPercentualTexto }}
      </strong>
      <span class="painel-esportivo-disco-capacidade">
        {{ discoUsadaTexto }} / {{ discoTotalTexto }}
      </span>
    </aside>

    <article class="painel-esportivo-instrumento">
      <ContaGiros
        titulo="RAM"
        :valor="ramPercentual"
        cor="#ff9f12"
        :detalhes="detalhesRam"
      />
    </article>
  </section>
</template>

<script setup lang="ts">
  import { computed } from 'vue';
  import ContaGiros from '@/components/monitoramento/painel/ContaGiros.vue';
  import { useMonitoramentoStore } from '@/stores/monitoramento';
  import { formatarDecimal } from '@/utils/formatarNumero';

  const monitoramentoStore = useMonitoramentoStore();

  const formatarGb = (bytes: number | null): string =>
    bytes === null ? '--' : `${formatarDecimal(bytes / 1024 ** 3)} GB`;

  const cpuPercentual = computed(
    () => monitoramentoStore.snapshot?.cpuPercentual ?? null
  );

  const cpuNome = computed(() => monitoramentoStore.snapshot?.cpuNome || '--');

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

  const ramUsadaTexto = computed(() => formatarGb(ramUsadaBytes.value));
  const ramTotalTexto = computed(() => formatarGb(ramTotalBytes.value));

  const detalhesCpu = computed(() => [
    {
      icone: 'mdi-thermometer',
      cor: '#ff9f12',
      texto: cpuTemperaturaTexto.value
    },
    {
      icone: 'mdi-speedometer',
      cor: '#74d94b',
      texto: cpuFrequenciaTexto.value
    }
  ]);

  const detalhesRam = computed(() => [
    {
      icone: 'mdi-memory',
      cor: '#ff9f12',
      texto: ramUsadaTexto.value
    },
    {
      icone: 'mdi-memory',
      cor: '#899092',
      texto: ramTotalTexto.value
    }
  ]);

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

  const discoPercentualTexto = computed(() =>
    discoPercentual.value === null
      ? '--'
      : `${formatarDecimal(discoPercentual.value)}%`
  );

  const discoUsadaTexto = computed(() => formatarGb(discoUsadaBytes.value));
  const discoTotalTexto = computed(() => formatarGb(discoTotalBytes.value));
</script>

<style scoped>
  .painel-esportivo {
    flex: 1;
    position: relative;
    container-type: size;
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(145px, 0.48fr) minmax(0, 1fr);
    align-items: center;
    overflow: hidden;
    color: #f3f4f4;
    background:
      radial-gradient(
        circle at 50% 50%,
        rgba(255, 255, 255, 0.035),
        transparent 48%
      ),
      linear-gradient(180deg, #101314 0%, #050708 100%);
  }

  .painel-esportivo::before {
    content: '';
    position: absolute;
    inset: 0;
    opacity: 0.08;
    pointer-events: none;
    background:
      repeating-linear-gradient(
        135deg,
        rgba(255, 255, 255, 0.07) 0 1px,
        transparent 1px 7px
      ),
      repeating-linear-gradient(
        45deg,
        rgba(255, 255, 255, 0.035) 0 1px,
        transparent 1px 7px
      );
  }

  .painel-esportivo-instrumento {
    position: relative;
    z-index: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    min-width: 0;
    min-height: 0;
  }

  .painel-esportivo-instrumento .conta-giros {
    transform: translateY(10px);
  }

  .painel-esportivo-cpu-nome {
    position: absolute;
    z-index: 3;
    top: -6px;
    left: clamp(8px, 2vw, 28px);
    right: clamp(8px, 2vw, 28px);
    overflow: hidden;
    color: #cfd4d5;
    font-size: clamp(9px, 1.3vw, 14px);
    font-weight: 700;
    letter-spacing: 0.07em;
    text-align: center;
    text-overflow: ellipsis;
    text-transform: uppercase;
    white-space: nowrap;
  }

  .painel-esportivo-centro {
    z-index: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: clamp(4px, 0.9vh, 8px);
    padding: clamp(12px, 2.5vh, 24px) clamp(10px, 1.8vw, 22px);
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 12px;
    background:
      linear-gradient(135deg, rgba(255, 255, 255, 0.04), transparent 40%),
      rgba(5, 7, 8, 0.76);
    box-shadow:
      inset 0 1px rgba(255, 255, 255, 0.06),
      0 14px 30px rgba(0, 0, 0, 0.28);
    text-align: center;
  }

  .painel-esportivo-centro-rotulo {
    color: #747c7e;
    font-size: clamp(8px, 1vw, 10px);
    font-weight: 700;
    letter-spacing: 0.14em;
  }

  .painel-esportivo-sistema {
    max-width: 100%;
    overflow: hidden;
    color: #edf0f0;
    font-size: clamp(10px, 1.4vw, 15px);
    font-weight: 600;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .painel-esportivo-divisor {
    width: 100%;
    height: 1px;
    margin: clamp(3px, 0.8vh, 8px) 0;
    background: rgba(255, 255, 255, 0.11);
  }

  .painel-esportivo-disco-percentual {
    color: #d2d7d7;
    font-size: clamp(18px, 2.5vw, 28px);
    font-variant-numeric: tabular-nums;
  }

  .painel-esportivo-disco-capacidade {
    color: #899092;
    font-size: clamp(10px, 1.3vw, 14px);
    font-variant-numeric: tabular-nums;
    white-space: nowrap;
  }

  @media (orientation: portrait) {
    .painel-esportivo {
      grid-template-columns: 1fr;
      grid-template-rows: repeat(3, minmax(0, 1fr));
      overflow-y: auto;
    }
  }
</style>

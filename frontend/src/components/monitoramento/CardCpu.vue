<template>
  <section class="cockpit-card cockpit-cpu">
    <div class="d-flex align-center ga-2 mb-2">
      <v-icon
        :color="cor"
        size="small"
      >
        mdi-chip
      </v-icon>
      <span class="text-caption text-grey text-uppercase">CPU</span>
    </div>

    <div
      class="cockpit-cpu-nome"
      :title="nome"
    >
      {{ nome }}
    </div>

    <div class="cockpit-cpu-circuito">
      <v-progress-circular
        :model-value="percentual ?? 0"
        :size="170"
        :width="12"
        :color="cor"
      >
        <div class="cockpit-cpu-percentual">
          <span :class="`text-${cor}`">{{ percentualTexto }}</span>
          <span class="text-caption text-grey text-uppercase">uso atual</span>
        </div>
      </v-progress-circular>
    </div>

    <div class="cockpit-cpu-rodape">
      <div class="cockpit-cpu-item">
        <span class="text-caption text-grey">Frequência</span>
        <span class="cockpit-cpu-valor">{{ frequencia }}</span>
      </div>

      <div class="cockpit-cpu-item cockpit-cpu-item-direita">
        <span class="text-caption text-grey">Temperatura</span>
        <span class="cockpit-cpu-valor">{{ temperatura }}</span>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
  import { computed } from 'vue';
  import { corPorUso } from '@/utils/corUso';

  const props = defineProps<{
    nome: string;
    percentual: number | null;
    frequenciaMhz: number | null;
    temperaturaCelsius: number | null;
  }>();

  const cor = computed(() =>
    props.percentual === null ? 'primary' : corPorUso(props.percentual)
  );

  const percentualTexto = computed(() =>
    props.percentual === null ? '--' : `${props.percentual.toFixed(0)}%`
  );

  const frequencia = computed(() => {
    if (props.frequenciaMhz === null) return '--';
    return props.frequenciaMhz >= 1000
      ? `${(props.frequenciaMhz / 1000).toFixed(1)} GHz`
      : `${Math.round(props.frequenciaMhz)} MHz`;
  });

  const temperatura = computed(() =>
    props.temperaturaCelsius === null
      ? '--'
      : `${Math.round(props.temperaturaCelsius)}°C`
  );
</script>

<style scoped>
  .cockpit-card {
    background: rgb(var(--v-theme-surface));
    border: 1px solid rgba(var(--v-theme-on-surface), 0.08);
    border-radius: 16px;
    padding: 16px 20px;
    min-width: 0;
  }

  .cockpit-cpu {
    display: flex;
    flex-direction: column;
  }

  .cockpit-cpu-nome {
    font-size: 0.9rem;
    font-weight: 600;
    text-align: center;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .cockpit-cpu-circuito {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 8px 0;
    min-height: 0;
  }

  .cockpit-cpu-percentual {
    display: flex;
    flex-direction: column;
    align-items: center;
    line-height: 1.25;
  }

  .cockpit-cpu-percentual > span:first-child {
    font-size: 2.6rem;
    font-weight: 700;
    font-variant-numeric: tabular-nums;
  }

  .cockpit-cpu-rodape {
    display: flex;
    justify-content: space-between;
    gap: 16px;
  }

  .cockpit-cpu-item {
    display: flex;
    flex-direction: column;
  }

  .cockpit-cpu-item-direita {
    align-items: flex-end;
  }

  .cockpit-cpu-valor {
    font-size: 1.1rem;
    font-weight: 600;
    font-variant-numeric: tabular-nums;
  }
</style>

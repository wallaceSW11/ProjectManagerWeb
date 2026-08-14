<template>
  <section class="cockpit-card cockpit-circular">
    <div class="d-flex align-center ga-2 mb-2">
      <v-icon
        :color="cor"
        size="small"
      >
        {{ icone }}
      </v-icon>
      <span class="text-caption text-grey text-uppercase">{{ titulo }}</span>
    </div>

    <div
      v-if="nome"
      class="cockpit-circular-nome"
      :title="nome"
    >
      {{ nome }}
    </div>

    <div class="cockpit-circular-circuito">
      <v-progress-circular
        :model-value="percentual ?? 0"
        :size="170"
        :width="12"
        :color="cor"
      >
        <div class="cockpit-circular-percentual">
          <span :class="`text-${cor}`">{{ percentualTexto }}</span>
          <span class="text-caption text-grey text-uppercase">uso atual</span>
        </div>
      </v-progress-circular>
    </div>

    <div class="cockpit-circular-rodape">
      <div class="cockpit-circular-item">
        <span class="text-caption text-grey">{{ rotuloEsquerdo }}</span>
        <span class="cockpit-circular-valor">{{ valorEsquerdo }}</span>
      </div>

      <div class="cockpit-circular-item cockpit-circular-item-direita">
        <span class="text-caption text-grey">{{ rotuloDireito }}</span>
        <span class="cockpit-circular-valor">{{ valorDireito }}</span>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
  import { computed } from 'vue';
  import { corPorUso } from '@/utils/corUso';

  const props = defineProps<{
    icone: string;
    titulo: string;
    nome: string | null;
    percentual: number | null;
    rotuloEsquerdo: string;
    valorEsquerdo: string;
    rotuloDireito: string;
    valorDireito: string;
  }>();

  const cor = computed(() =>
    props.percentual === null ? 'primary' : corPorUso(props.percentual)
  );

  const percentualTexto = computed(() =>
    props.percentual === null ? '--' : `${props.percentual.toFixed(0)}%`
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

  .cockpit-circular {
    display: flex;
    flex-direction: column;
  }

  .cockpit-circular-nome {
    font-size: 0.9rem;
    font-weight: 600;
    text-align: center;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .cockpit-circular-circuito {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 8px 0;
    min-height: 0;
  }

  .cockpit-circular-percentual {
    display: flex;
    flex-direction: column;
    align-items: center;
    line-height: 1.25;
  }

  .cockpit-circular-percentual > span:first-child {
    font-size: 2.6rem;
    font-weight: 700;
    font-variant-numeric: tabular-nums;
  }

  .cockpit-circular-rodape {
    display: flex;
    justify-content: space-between;
    gap: 16px;
  }

  .cockpit-circular-item {
    display: flex;
    flex-direction: column;
  }

  .cockpit-circular-item-direita {
    align-items: flex-end;
  }

  .cockpit-circular-valor {
    font-size: 1.1rem;
    font-weight: 600;
    font-variant-numeric: tabular-nums;
  }
</style>

<template>
  <section class="cockpit-card">
    <div class="d-flex align-center ga-2 mb-2">
      <v-icon
        :color="cor"
        size="small"
      >
        {{ icone }}
      </v-icon>
      <span class="text-caption text-grey text-uppercase">{{ titulo }}</span>
    </div>

    <div :class="`cockpit-valor text-${cor}`">
      {{ valor }}
    </div>

    <div class="text-caption text-grey">
      {{ detalhe }}
    </div>

    <v-progress-linear
      v-if="percentual !== null"
      :model-value="percentual"
      :color="cor"
      class="mt-2"
      height="8"
      rounded
    />
  </section>
</template>

<script setup lang="ts">
  defineProps<{
    icone: string;
    titulo: string;
    valor: string;
    detalhe: string;
    percentual: number | null;
    cor: string;
  }>();
</script>

<style scoped>
  .cockpit-card {
    background: rgb(var(--v-theme-surface));
    border: 1px solid rgba(var(--v-theme-on-surface), 0.08);
    border-radius: 16px;
    padding: 16px 20px;
    min-width: 0;
  }

  .cockpit-valor {
    font-size: 2.4rem;
    font-weight: 700;
    line-height: 1.1;
    font-variant-numeric: tabular-nums;
  }
</style>

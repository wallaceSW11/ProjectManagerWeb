<template>
  <v-dialog
    v-model="modelValue"
    width="80%"
    max-width="640px"
    content-class="conteudo-modal-processos"
  >
    <div class="modal-processos">
      <header class="modal-processos-cabecalho">
        <v-icon
          :color="corDestaque"
          size="16"
        >
          {{ icone }}
        </v-icon>
        <span class="modal-processos-titulo">{{ titulo }}</span>
        <v-btn
          class="modal-processos-fechar"
          icon
          variant="plain"
          size="x-small"
          :ripple="false"
          @click="fechar"
        >
          <v-icon size="16">mdi-close</v-icon>
        </v-btn>
      </header>

      <main
        v-if="possuiProcessos"
        class="modal-processos-lista"
      >
        <div
          v-for="(processo, indice) in processos"
          :key="indice"
          class="modal-processos-item"
        >
          <span class="modal-processos-posicao">{{ indice + 1 }}</span>
          <span class="modal-processos-nome">{{ processo.nome }}</span>
          <span class="modal-processos-valores">
            <span
              v-if="tipo === 'ram'"
              class="modal-processos-memoria"
            >
              {{ formatarGb(processo.memoriaBytes) }}
            </span>
            <strong
              class="modal-processos-percentual"
              :style="{ color: corDestaque }"
            >
              {{ formatarPercentual(processo.percentual) }}
            </strong>
          </span>
        </div>
      </main>

      <main
        v-else
        class="modal-processos-vazio"
      >
        {{ textoVazio }}
      </main>
    </div>
  </v-dialog>
</template>

<script setup lang="ts">
  import { computed, onBeforeUnmount, ref, watch } from 'vue';
  import { useMonitoramentoStore } from '@/stores/monitoramento';
  import { formatarDecimal } from '@/utils/formatarNumero';
  import type { TipoTopProcessos } from '@/types';

  const INTERVALO_ATUALIZACAO_MS = 2000;

  const props = defineProps<{
    tipo: TipoTopProcessos;
  }>();

  const modelValue = defineModel<boolean>({ default: false });

  const monitoramentoStore = useMonitoramentoStore();
  const timer = ref<ReturnType<typeof setInterval> | null>(null);

  const titulo = computed(() =>
    props.tipo === 'cpu' ? 'Top processos — CPU' : 'Top processos — Memória'
  );

  const icone = computed(() =>
    props.tipo === 'cpu' ? 'mdi-cpu-64-bit' : 'mdi-memory'
  );

  const corDestaque = computed(() =>
    props.tipo === 'cpu' ? '#74d94b' : '#ff9f12'
  );

  const processos = computed(() => monitoramentoStore.processos[props.tipo]);

  const possuiProcessos = computed(() => processos.value.length > 0);

  const textoVazio = computed(() =>
    monitoramentoStore.carregandoProcessos
      ? 'Coletando processos...'
      : monitoramentoStore.erroProcessos || 'Nenhum processo encontrado.'
  );

  const fechar = (): void => {
    modelValue.value = false;
  };

  const formatarPercentual = (percentual: number): string =>
    `${formatarDecimal(percentual)}%`;

  const formatarGb = (bytes: number | null): string =>
    bytes === null ? '' : `${formatarDecimal(bytes / 1024 ** 3)} GB`;

  const iniciarPolling = (): void => {
    void monitoramentoStore.carregarTopProcessos(props.tipo);
    timer.value = setInterval(() => {
      void monitoramentoStore.carregarTopProcessos(props.tipo);
    }, INTERVALO_ATUALIZACAO_MS);
  };

  const pararPolling = (): void => {
    if (timer.value === null) return;
    clearInterval(timer.value);
    timer.value = null;
  };

  watch(modelValue, aberto => {
    if (aberto) iniciarPolling();
    else pararPolling();
  });

  onBeforeUnmount(pararPolling);
</script>

<style scoped>
  .modal-processos {
    display: flex;
    flex-direction: column;
    height: 100%;
    max-height: 100%;
    overflow: hidden;
    border: 1px solid rgba(255, 255, 255, 0.12);
    border-radius: 12px;
    background:
      linear-gradient(135deg, rgba(255, 255, 255, 0.04), transparent 40%),
      #0b0e0f;
    box-shadow:
      inset 0 1px rgba(255, 255, 255, 0.06),
      0 24px 48px rgba(0, 0, 0, 0.6);
    color: #f3f4f4;
  }

  .modal-processos-cabecalho {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 3px 10px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    background: rgba(5, 7, 8, 0.72);
  }

  .modal-processos-titulo {
    flex: 1;
    overflow: hidden;
    color: #edf0f0;
    font-size: 0.72rem;
    font-weight: 700;
    letter-spacing: 0.06em;
    line-height: 1.2;
    text-overflow: ellipsis;
    text-transform: uppercase;
    white-space: nowrap;
  }

  .modal-processos-fechar {
    color: #747c7e;
  }

  .modal-processos-lista {
    flex: 1;
    overflow-y: auto;
    padding: 2px 10px 6px;
  }

  .modal-processos-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 4px 0;
    border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  }

  .modal-processos-item:last-child {
    border-bottom: none;
  }

  .modal-processos-posicao {
    flex-shrink: 0;
    width: 20px;
    color: #747c7e;
    font-size: 0.72rem;
    font-weight: 700;
    font-style: italic;
    text-align: center;
  }

  .modal-processos-nome {
    flex: 1;
    min-width: 0;
    overflow: hidden;
    color: #cfd4d5;
    font-size: 0.8rem;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .modal-processos-valores {
    display: flex;
    align-items: baseline;
    gap: 8px;
    flex-shrink: 0;
  }

  .modal-processos-percentual {
    font-size: 0.82rem;
    font-weight: 700;
    font-variant-numeric: tabular-nums;
  }

  .modal-processos-memoria {
    color: #899092;
    font-size: 0.72rem;
    font-variant-numeric: tabular-nums;
  }

  .modal-processos-vazio {
    padding: 28px 16px;
    color: #899092;
    font-size: 0.85rem;
    font-style: italic;
    text-align: center;
  }
</style>

<style>
  .v-dialog > .v-overlay__content.conteudo-modal-processos {
    height: 100%;
    max-height: 100%;
    margin: 0;
  }
</style>

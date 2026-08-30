<template>
  <v-dialog
    v-model="modelValue"
    width="40%"
    max-width="640px"
  >
    <div class="modal-processos">
      <header class="modal-processos-cabecalho">
        <v-icon
          :color="corBarra"
          size="20"
        >
          {{ icone }}
        </v-icon>
        <span class="modal-processos-titulo">{{ titulo }}</span>
        <v-btn
          class="modal-processos-fechar"
          icon
          variant="plain"
          size="small"
          :ripple="false"
          @click="fechar"
        >
          <v-icon size="18">mdi-close</v-icon>
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
          <div class="modal-processos-linha">
            <span class="modal-processos-posicao">{{ indice + 1 }}</span>
            <span class="modal-processos-nome">{{ processo.nome }}</span>
            <span class="modal-processos-valores">
              <strong
                class="modal-processos-percentual"
                :style="{ color: corBarra }"
              >
                {{ formatarPercentual(processo.percentual) }}
              </strong>
              <span
                v-if="tipo === 'ram'"
                class="modal-processos-memoria"
              >
                {{ formatarGb(processo.memoriaBytes) }}
              </span>
            </span>
          </div>
          <div class="modal-processos-barra">
            <div
              class="modal-processos-barra-preenchida"
              :style="{
                width: larguraBarra(processo.percentual),
                backgroundColor: corBarra
              }"
            />
          </div>
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

  const corBarra = computed(() =>
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

  const larguraBarra = (percentual: number): string => {
    const minimoVisivel = percentual > 0 ? 1.5 : 0;
    return `${Math.min(100, Math.max(minimoVisivel, percentual))}%`;
  };

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
    max-height: 80vh;
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
    gap: 10px;
    padding: 12px 16px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    background: rgba(5, 7, 8, 0.72);
  }

  .modal-processos-titulo {
    flex: 1;
    overflow: hidden;
    color: #edf0f0;
    font-size: 0.95rem;
    font-weight: 700;
    letter-spacing: 0.06em;
    text-overflow: ellipsis;
    text-transform: uppercase;
    white-space: nowrap;
  }

  .modal-processos-fechar {
    color: #747c7e;
  }

  .modal-processos-lista {
    overflow-y: auto;
    padding: 8px 16px 14px;
  }

  .modal-processos-item {
    padding: 9px 0;
    border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  }

  .modal-processos-item:last-child {
    border-bottom: none;
  }

  .modal-processos-linha {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .modal-processos-posicao {
    flex-shrink: 0;
    width: 22px;
    color: #747c7e;
    font-size: 0.78rem;
    font-weight: 700;
    font-style: italic;
    text-align: center;
  }

  .modal-processos-nome {
    flex: 1;
    min-width: 0;
    overflow: hidden;
    color: #cfd4d5;
    font-size: 0.85rem;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .modal-processos-valores {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    gap: 1px;
    flex-shrink: 0;
  }

  .modal-processos-percentual {
    font-size: 0.88rem;
    font-weight: 700;
    font-variant-numeric: tabular-nums;
  }

  .modal-processos-memoria {
    color: #899092;
    font-size: 0.72rem;
    font-variant-numeric: tabular-nums;
  }

  .modal-processos-barra {
    height: 4px;
    margin: 6px 0 0 32px;
    overflow: hidden;
    border-radius: 2px;
    background: rgba(255, 255, 255, 0.08);
  }

  .modal-processos-barra-preenchida {
    height: 100%;
    border-radius: 2px;
    transition: width 0.5s ease;
  }

  .modal-processos-vazio {
    padding: 28px 16px;
    color: #899092;
    font-size: 0.85rem;
    font-style: italic;
    text-align: center;
  }
</style>

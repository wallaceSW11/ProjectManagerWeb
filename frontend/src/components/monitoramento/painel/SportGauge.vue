<template>
  <div class="sport-gauge">
    <div
      class="sport-gauge-titulo"
      :title="titulo"
    >
      {{ titulo }}
    </div>

    <div class="sport-gauge-instrumento">
      <svg viewBox="0 0 200 200">
        <defs>
          <linearGradient
            :id="idGradienteAro"
            x1="0"
            y1="0"
            x2="1"
            y2="1"
          >
            <stop
              offset="0%"
              stop-color="#4a5052"
            />
            <stop
              offset="50%"
              stop-color="#191d1e"
            />
            <stop
              offset="100%"
              stop-color="#3a3f41"
            />
          </linearGradient>
        </defs>

        <circle
          class="sport-gauge-aro"
          :stroke="`url(#${idGradienteAro})`"
          cx="100"
          cy="100"
          r="90"
          stroke-width="9"
        />

        <circle
          class="sport-gauge-trilha"
          cx="100"
          cy="100"
          r="78"
          pathLength="100"
          stroke-dasharray="82 100"
          transform="rotate(140 100 100)"
        />

        <path
          class="sport-gauge-alerta"
          :d="caminhoAlerta"
        />

        <g
          v-for="tick in ticks"
          :key="`tick-${tick.valor}`"
          class="sport-gauge-tick"
          :class="tickClasse(tick)"
          :transform="`rotate(${tick.angulo} 100 100)`"
        >
          <polygon :points="tick.pontos" />
        </g>

        <text
          v-for="numero in numeros"
          :key="`numero-${numero.valor}`"
          class="sport-gauge-numero"
          :x="numero.x"
          :y="numero.y"
          text-anchor="middle"
          dominant-baseline="central"
        >
          {{ numero.valor }}
        </text>

        <g
          class="sport-gauge-ponteiro"
          :style="{ transform: `rotate(${anguloPonteiro}deg)` }"
        >
          <line
            class="sport-gauge-ponteiro-linha"
            :stroke="cor"
            x1="100"
            y1="100"
            x2="100"
            y2="26"
          />
          <circle
            class="sport-gauge-ponteiro-centro"
            cx="100"
            cy="100"
            r="6"
          />
          <circle
            class="sport-gauge-ponteiro-eixo"
            :fill="cor"
            cx="100"
            cy="100"
            r="2.4"
          />
        </g>
      </svg>

      <div
        class="sport-gauge-valor"
        :style="{ color: cor }"
      >
        {{ percentualTexto }}
      </div>
    </div>

    <div class="sport-gauge-secundario">{{ secundario }}</div>
  </div>
</template>

<script setup lang="ts">
  import { computed, onBeforeUnmount, ref, useId, watch } from 'vue';

  const ANGULO_INICIO = -140;
  const ANGULO_FIM = 140;
  const RAIO_ARCO = 78;
  const RAIO_NUMEROS = 60;
  const DURACAO_ANIMACAO_MS = 600;

  const props = defineProps<{
    titulo: string;
    valor: number | null;
    cor: string;
    secundario: string;
  }>();

  const idGradienteAro = useId();

  const limitar = (valor: number): number => Math.min(100, Math.max(0, valor));

  const valorPonteiro = computed(() => limitar(props.valor ?? 0));

  const anguloPonteiro = computed(
    () =>
      ANGULO_INICIO + (valorPonteiro.value / 100) * (ANGULO_FIM - ANGULO_INICIO)
  );

  const pontoNoArco = (angulo: number): { x: number; y: number } => {
    const radianos = (angulo * Math.PI) / 180;
    return {
      x: 100 + RAIO_ARCO * Math.sin(radianos),
      y: 100 - RAIO_ARCO * Math.cos(radianos)
    };
  };

  const caminhoAlerta = computed(() => {
    const inicio = pontoNoArco(
      ANGULO_INICIO + 0.9 * (ANGULO_FIM - ANGULO_INICIO)
    );
    const fim = pontoNoArco(ANGULO_FIM);
    return `M ${inicio.x} ${inicio.y} A ${RAIO_ARCO} ${RAIO_ARCO} 0 0 1 ${fim.x} ${fim.y}`;
  });

  const ticks = computed(() =>
    Array.from({ length: 21 }, (_, i) => {
      const valor = i * 5;
      const major = valor % 10 === 0;
      return {
        valor,
        angulo: ANGULO_INICIO + (valor / 100) * (ANGULO_FIM - ANGULO_INICIO),
        major,
        vermelha: valor >= 90,
        pontos: major ? '98.4,23 100,11 101.6,23' : '99.2,19 100,14 100.8,19'
      };
    })
  );

  const numeros = computed(() =>
    [0, 20, 40, 60, 80, 100].map(valor => {
      const radianos =
        ((ANGULO_INICIO + (valor / 100) * (ANGULO_FIM - ANGULO_INICIO)) *
          Math.PI) /
        180;
      return {
        valor,
        x: 100 + RAIO_NUMEROS * Math.sin(radianos),
        y: 100 - RAIO_NUMEROS * Math.cos(radianos)
      };
    })
  );

  const tickClasse = (tick: { major: boolean; vermelha: boolean }): string =>
    tick.vermelha
      ? 'sport-gauge-tick-vermelha'
      : tick.major
        ? 'sport-gauge-tick-major'
        : '';

  const exibido = ref(0);
  let frameAnimacao: number | null = null;

  watch(
    () => props.valor,
    novo => {
      if (frameAnimacao !== null) cancelAnimationFrame(frameAnimacao);

      const inicio = exibido.value;
      const alvo = novo === null ? 0 : limitar(novo);
      const comeco = performance.now();

      const passo = (agora: number): void => {
        const progresso = Math.min((agora - comeco) / DURACAO_ANIMACAO_MS, 1);
        const suavizado = 1 - Math.pow(1 - progresso, 3);
        exibido.value = inicio + (alvo - inicio) * suavizado;

        if (progresso < 1) frameAnimacao = requestAnimationFrame(passo);
        else frameAnimacao = null;
      };

      frameAnimacao = requestAnimationFrame(passo);
    },
    { immediate: true }
  );

  onBeforeUnmount(() => {
    if (frameAnimacao !== null) cancelAnimationFrame(frameAnimacao);
  });

  const percentualTexto = computed(() =>
    props.valor === null ? '--' : `${Math.round(exibido.value)}%`
  );
</script>

<style scoped>
  .sport-gauge {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 6px;
    min-width: 0;
  }

  .sport-gauge-instrumento {
    position: relative;
    width: min(58vw, 72vh);
    aspect-ratio: 1;
    filter: drop-shadow(0 16px 28px rgba(0, 0, 0, 0.5));
  }

  .sport-gauge-instrumento svg {
    width: 100%;
    height: 100%;
    overflow: visible;
  }

  .sport-gauge-instrumento::after {
    content: '';
    position: absolute;
    inset: 0;
    border-radius: 50%;
    pointer-events: none;
    background:
      radial-gradient(
        circle at 50% 28%,
        rgba(255, 255, 255, 0.07),
        transparent 42%
      ),
      radial-gradient(circle at 50% 55%, transparent 60%, rgba(0, 0, 0, 0.55));
  }

  .sport-gauge-aro {
    fill: none;
  }

  .sport-gauge-trilha {
    fill: none;
    stroke: #262b2d;
    stroke-width: 7;
  }

  .sport-gauge-alerta {
    fill: none;
    stroke: #ff3b30;
    stroke-width: 6;
    stroke-linecap: round;
    filter: drop-shadow(0 0 4px rgba(255, 59, 48, 0.4));
  }

  .sport-gauge-tick polygon {
    fill: #687073;
    opacity: 0.85;
  }

  .sport-gauge-tick-major polygon {
    fill: #c2c7c8;
    opacity: 0.95;
  }

  .sport-gauge-tick-vermelha polygon {
    fill: #ff3b30;
    opacity: 1;
  }

  .sport-gauge-ponteiro {
    transform-origin: 100px 100px;
    transition: transform 0.7s cubic-bezier(0.22, 0.61, 0.36, 1);
  }

  .sport-gauge-ponteiro-linha {
    stroke-width: 3;
    stroke-linecap: round;
    filter: drop-shadow(0 0 5px rgba(255, 255, 255, 0.2));
  }

  .sport-gauge-ponteiro-centro {
    fill: #181c1d;
    stroke: #d8dcdc;
    stroke-width: 2;
  }

  .sport-gauge-ponteiro-eixo {
    filter: drop-shadow(0 0 3px rgba(255, 255, 255, 0.35));
  }

  .sport-gauge-titulo {
    color: #cdd1d2;
    font-size: clamp(10px, 1.4vw, 13px);
    font-weight: 700;
    font-style: italic;
    letter-spacing: 0.08em;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 100%;
    text-align: center;
    margin-bottom: 4px;
  }

  .sport-gauge-valor {
    position: absolute;
    top: 53%;
    left: 0;
    right: 0;
    text-align: center;
    font-size: clamp(28px, 4.2vw, 46px);
    line-height: 1;
    font-weight: 800;
    font-style: italic;
    letter-spacing: -0.03em;
    text-shadow: 0 0 24px rgba(255, 255, 255, 0.14);
  }

  .sport-gauge-numero {
    fill: #899092;
    font-size: 12px;
    font-weight: 700;
    font-style: italic;
  }

  .sport-gauge-secundario {
    color: #899092;
    font-size: clamp(10px, 1.4vw, 13px);
    font-variant-numeric: tabular-nums;
    letter-spacing: 0.03em;
    white-space: nowrap;
  }

  @media (max-height: 300px) {
    .sport-gauge-valor {
      top: 51%;
    }
  }
</style>

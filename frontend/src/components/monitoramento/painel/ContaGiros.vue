<template>
  <div class="conta-giros">
    <svg
      class="conta-giros-svg"
      viewBox="0 0 200 200"
      :aria-label="titulo"
    >
      <defs>
        <radialGradient :id="idFundo" cx="50%" cy="42%" r="62%">
          <stop offset="0%" stop-color="#171b1c" />
          <stop offset="68%" stop-color="#090b0c" />
          <stop offset="100%" stop-color="#030405" />
        </radialGradient>
        <linearGradient :id="idAro" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stop-color="#7a8385" />
          <stop offset="16%" stop-color="#252a2b" />
          <stop offset="50%" stop-color="#090a0b" />
          <stop offset="84%" stop-color="#343a3b" />
          <stop offset="100%" stop-color="#0d0f10" />
        </linearGradient>
        <filter :id="idBrilho" x="-30%" y="-30%" width="160%" height="160%">
          <feGaussianBlur stdDeviation="1.4" result="blur" />
          <feMerge>
            <feMergeNode in="blur" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>
      </defs>

      <circle class="conta-giros-sombra" cx="100" cy="100" r="94" />
      <circle class="conta-giros-aro" :stroke="`url(#${idAro})`" cx="100" cy="100" r="90" />
      <circle class="conta-giros-fundo" :fill="`url(#${idFundo})`" cx="100" cy="100" r="83" />
      <path
        class="conta-giros-trilha"
        :d="caminhoTrilha"
      />
      <path
        class="conta-giros-progresso"
        :stroke="cor"
        :filter="`url(#${idBrilho})`"
        :d="caminhoProgresso"
      />

      <g
        v-for="tick in ticks"
        :key="tick.valor"
        :transform="`rotate(${tick.angulo} 100 100)`"
      >
        <line
          :class="['conta-giros-tick', { 'conta-giros-tick-maior': tick.maior, 'conta-giros-tick-alerta': tick.alerta }]"
          x1="100"
          :y1="tick.maior ? 22 : 25"
          x2="100"
          :y2="tick.maior ? 29 : 28"
        />
      </g>

      <text
        v-for="numero in numeros"
        :key="numero.valor"
        class="conta-giros-numero"
        :x="numero.x"
        :y="numero.y"
        dominant-baseline="central"
        text-anchor="middle"
      >
        {{ numero.valor }}
      </text>

      <g
        class="conta-giros-ponteiro"
        :style="{ transform: `rotate(${anguloPonteiro}deg)` }"
      >
        <line
          class="conta-giros-ponteiro-linha"
          :stroke="cor"
          x1="100"
          y1="100"
          x2="100"
          y2="34"
        />
        <circle class="conta-giros-ponteiro-centro" cx="100" cy="100" r="7" />
        <circle :fill="cor" cx="100" cy="100" r="3" />
      </g>
    </svg>

    <span class="conta-giros-titulo">{{ titulo }}</span>
    <div class="conta-giros-informacao">
      <span class="conta-giros-valor" :style="{ color: cor }">{{ percentualTexto }}</span>
      <div v-if="detalhes?.length" class="conta-giros-detalhes">
        <span v-for="detalhe in detalhes" :key="detalhe.texto" class="conta-giros-detalhe">
          <v-icon :color="detalhe.cor" size="17">{{ detalhe.icone }}</v-icon>
          {{ detalhe.texto }}
        </span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed, onBeforeUnmount, ref, useId, watch } from 'vue';

  const ANGULO_INICIO = -140;
  const ANGULO_FIM = 140;
  const RAIO_NUMEROS = 59;
  const DURACAO_ANIMACAO_MS = 650;

  const props = defineProps<{
    titulo: string;
    valor: number | null;
    cor: string;
    detalhes?: {
      icone: string;
      cor: string;
      texto: string;
    }[];
  }>();

  const idFundo = useId();
  const idAro = useId();
  const idBrilho = useId();
  const exibido = ref(0);
  let frameAnimacao: number | null = null;

  const limitar = (valor: number): number => Math.min(100, Math.max(0, valor));

  const anguloPonteiro = computed(
    () => ANGULO_INICIO + (exibido.value / 100) * (ANGULO_FIM - ANGULO_INICIO)
  );

  const pontoNoArco = (angulo: number): { x: number; y: number } => {
    const radianos = (angulo * Math.PI) / 180;
    return {
      x: 100 + 78 * Math.sin(radianos),
      y: 100 - 78 * Math.cos(radianos)
    };
  };

  const caminhoArco = (inicio: number, fim: number): string => {
    const pontoInicio = pontoNoArco(inicio);
    const pontoFim = pontoNoArco(fim);
    const arcoMaior = Math.abs(fim - inicio) > 180 ? 1 : 0;
    return `M ${pontoInicio.x} ${pontoInicio.y} A 78 78 0 ${arcoMaior} 1 ${pontoFim.x} ${pontoFim.y}`;
  };

  const caminhoTrilha = computed(() => caminhoArco(ANGULO_INICIO, ANGULO_FIM));

  const caminhoProgresso = computed(() => {
    if (exibido.value <= 0) return '';
    return caminhoArco(ANGULO_INICIO, anguloPonteiro.value);
  });

  const ticks = computed(() =>
    Array.from({ length: 21 }, (_, indice) => {
      const valor = indice * 5;
      return {
        valor,
        angulo: ANGULO_INICIO + (valor / 100) * (ANGULO_FIM - ANGULO_INICIO),
        maior: valor % 10 === 0,
        alerta: valor >= 90
      };
    })
  );

  const numeros = computed(() =>
    [0, 20, 40, 60, 80, 100].map(valor => {
      const angulo = ANGULO_INICIO + (valor / 100) * (ANGULO_FIM - ANGULO_INICIO);
      const radianos = (angulo * Math.PI) / 180;
      return {
        valor,
        x: 100 + RAIO_NUMEROS * Math.sin(radianos),
        y: 100 - RAIO_NUMEROS * Math.cos(radianos)
      };
    })
  );

  const percentualTexto = computed(() =>
    props.valor === null ? '--' : `${Math.round(exibido.value)}%`
  );

  watch(
    () => props.valor,
    valor => {
      if (frameAnimacao !== null) cancelAnimationFrame(frameAnimacao);

      const inicio = exibido.value;
      const alvo = limitar(valor ?? 0);
      const comeco = performance.now();

      const animar = (agora: number): void => {
        const progresso = Math.min((agora - comeco) / DURACAO_ANIMACAO_MS, 1);
        exibido.value = inicio + (alvo - inicio) * (1 - Math.pow(1 - progresso, 3));
        frameAnimacao = progresso < 1 ? requestAnimationFrame(animar) : null;
      };

      frameAnimacao = requestAnimationFrame(animar);
    },
    { immediate: true }
  );

  onBeforeUnmount(() => {
    if (frameAnimacao !== null) cancelAnimationFrame(frameAnimacao);
  });
</script>

<style scoped>
  .conta-giros {
    position: relative;
    width: min(100%, 42vw, 78vh, calc(100cqh - 48px));
    aspect-ratio: 1;
    filter: drop-shadow(0 18px 24px rgba(0, 0, 0, 0.58));
  }

  .conta-giros-svg {
    width: 100%;
    height: 100%;
    overflow: visible;
  }

  .conta-giros-sombra {
    fill: #020303;
    stroke: #000;
    stroke-width: 2;
  }

  .conta-giros-aro {
    fill: none;
    stroke-width: 8;
  }

  .conta-giros-fundo {
    stroke: rgba(255, 255, 255, 0.08);
    stroke-width: 1;
  }

  .conta-giros-trilha,
  .conta-giros-progresso {
    fill: none;
    stroke-width: 4;
    stroke-linecap: round;
  }

  .conta-giros-trilha {
    stroke: #252a2b;
  }

  .conta-giros-tick {
    stroke: #707879;
    stroke-width: 1.3;
    opacity: 0.78;
  }

  .conta-giros-tick-maior {
    stroke: #d9dddd;
    stroke-width: 1.9;
    opacity: 0.95;
  }

  .conta-giros-tick-alerta {
    stroke: #ff3b30;
    opacity: 1;
  }

  .conta-giros-numero {
    fill: #e0e3e3;
    font-size: 12px;
    font-weight: 700;
    font-style: italic;
  }

  .conta-giros-ponteiro {
    transform-origin: 100px 100px;
  }

  .conta-giros-ponteiro-linha {
    stroke-width: 3.2;
    stroke-linecap: round;
    filter: drop-shadow(0 0 4px rgba(255, 255, 255, 0.35));
  }

  .conta-giros-ponteiro-centro {
    fill: #15191a;
    stroke: #d7dbdc;
    stroke-width: 1.6;
  }

  .conta-giros-informacao {
    position: absolute;
    inset: 52% 0 auto;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    line-height: 1;
  }

  .conta-giros-valor {
    font-size: clamp(28px, 4.8vw, 52px);
    font-weight: 800;
    font-style: italic;
    letter-spacing: -0.06em;
    text-shadow: 0 0 18px color-mix(in srgb, currentColor 24%, transparent);
  }

  .conta-giros-detalhes {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 3px;
    margin-top: 7px;
  }

  .conta-giros-detalhe {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    color: #a6adaf;
    font-size: clamp(10px, 1.35vw, 14px);
    font-variant-numeric: tabular-nums;
    white-space: nowrap;
  }

  .conta-giros-titulo {
    position: absolute;
    inset: 32% 0 auto;
    text-align: center;
    color: #e1e4e4;
    font-size: clamp(11px, 1.6vw, 16px);
    font-weight: 700;
    letter-spacing: 0.14em;
  }
</style>

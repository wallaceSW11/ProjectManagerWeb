<template>
  <!-- container fixo no topo direito -->
  <div class="toast-container">
    <transition-group
      name="slide"
      tag="div"
    >
      <div
        v-for="toast in toasts"
        :key="toast.id"
        :class="['toast', toast.tipo]"
      >
        <strong>{{ toast.titulo }}</strong>
        <span v-if="toast.mensagem">{{ toast.mensagem }}</span>
      </div>
    </transition-group>
  </div>
</template>

<script lang="ts">
  import { ref, onMounted, onBeforeUnmount, defineComponent } from 'vue';
  import eventBus from '@/utils/eventBus';
  import type { NotificacaoTipo } from '@/types';

  interface Toast extends NotificacaoTipo {
    id: number;
  }

  const toasts = ref<Toast[]>([]);
  let idCounter = 0;

  const DURACAO_POR_TIPO: Record<NotificacaoTipo['tipo'], number> = {
    sucesso: 3000,
    aviso: 5000,
    erro: 8000
  };

  export default defineComponent({
    name: 'ToastNotificacao',
    setup() {
      const handleNotificar = ({
        tipo,
        titulo,
        mensagem
      }: NotificacaoTipo): void => {
        const id = idCounter++;
        toasts.value.push({ id, tipo, titulo, mensagem });

        setTimeout(() => {
          const index = toasts.value.findIndex(t => t.id === id);
          if (index !== -1) toasts.value.splice(index, 1);
        }, DURACAO_POR_TIPO[tipo]);
      };

      onMounted(() => eventBus.on('notificar', handleNotificar));
      onBeforeUnmount(() => eventBus.off('notificar', handleNotificar));

      return { toasts };
    }
  });
</script>

<style scoped>
  .toast-container {
    position: fixed;
    top: 1rem;
    right: 1rem;
    z-index: 9999;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  /* cada toast */
  .toast {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
    min-width: 220px;
    max-width: 380px;
    padding: 1rem;
    border-radius: 8px;
    color: white;
    font-weight: 500;
    box-shadow: 0 4px 10px rgba(0, 0, 0, 0.2);
  }

  .toast strong {
    font-size: 0.95rem;
  }

  .toast span {
    font-size: 0.85rem;
    font-weight: 400;
    line-height: 1.35;
    white-space: pre-line;
    word-break: break-word;
  }

  /* cores por tipo */
  .toast.sucesso {
    background-color: #4caf50;
  }
  .toast.aviso {
    background-color: #ff9800;
  }
  .toast.erro {
    background-color: #f44336;
  }

  /* animação slide da direita para esquerda */
  .slide-enter-from,
  .slide-leave-to {
    transform: translateX(100%);
    opacity: 0;
  }
  .slide-enter-active,
  .slide-leave-active {
    transition: all 0.4s ease;
  }
</style>

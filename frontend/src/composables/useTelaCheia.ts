import {
  computed,
  onBeforeUnmount,
  onMounted,
  ref,
  type ComputedRef,
  type Ref
} from 'vue';

interface ElementoComWebkit {
  webkitRequestFullscreen?: () => Promise<void>;
}

interface DocumentoComWebkit {
  webkitExitFullscreen?: () => Promise<void>;
  webkitFullscreenElement?: Element | null;
}

interface UseTelaCheiaReturn {
  emTelaCheia: Ref<boolean>;
  suportaTelaCheia: ComputedRef<boolean>;
  alternarTelaCheia: () => Promise<void>;
}

export function useTelaCheia(): UseTelaCheiaReturn {
  const emTelaCheia = ref(false);

  const atualizarEstado = (): void => {
    const documento = document as DocumentoComWebkit;
    emTelaCheia.value = Boolean(
      document.fullscreenElement || documento.webkitFullscreenElement
    );
  };

  const alternarTelaCheia = async (): Promise<void> => {
    const documento = document as DocumentoComWebkit;

    if (emTelaCheia.value) {
      if (document.exitFullscreen) await document.exitFullscreen();
      else await documento.webkitExitFullscreen?.();
      return;
    }

    const raiz = document.documentElement as HTMLElement & ElementoComWebkit;
    if (raiz.requestFullscreen) await raiz.requestFullscreen();
    else await raiz.webkitRequestFullscreen?.();
  };

  const suportaTelaCheia = computed(() => {
    const raiz = document.documentElement as HTMLElement & ElementoComWebkit;
    return Boolean(raiz.requestFullscreen || raiz.webkitRequestFullscreen);
  });

  onMounted(() => {
    document.addEventListener('fullscreenchange', atualizarEstado);
    document.addEventListener('webkitfullscreenchange', atualizarEstado);
  });

  onBeforeUnmount(() => {
    document.removeEventListener('fullscreenchange', atualizarEstado);
    document.removeEventListener('webkitfullscreenchange', atualizarEstado);
  });

  return { emTelaCheia, suportaTelaCheia, alternarTelaCheia };
}

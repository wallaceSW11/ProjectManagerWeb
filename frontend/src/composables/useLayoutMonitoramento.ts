import { computed, ref, type ComputedRef, type Ref } from 'vue';
import { LAYOUT_MONITORAMENTO } from '@/constants/geral-constants';

const CHAVE_STORAGE = 'pmw-monitor-layout';

interface UseLayoutMonitoramentoReturn {
  layoutAtual: Ref<string>;
  ehCockpit: ComputedRef<boolean>;
  ehPainelEsportivo: ComputedRef<boolean>;
  selecionarLayout: (valor: string) => void;
}

const layoutAtual = ref<string>(
  localStorage.getItem(CHAVE_STORAGE) ?? LAYOUT_MONITORAMENTO.PADRAO.valor
);

export function useLayoutMonitoramento(): UseLayoutMonitoramentoReturn {
  const ehCockpit = computed(
    () => layoutAtual.value === LAYOUT_MONITORAMENTO.COCKPIT.valor
  );

  const ehPainelEsportivo = computed(
    () => layoutAtual.value === LAYOUT_MONITORAMENTO.PAINEL_ESPORTIVO.valor
  );

  const selecionarLayout = (valor: string): void => {
    layoutAtual.value = valor;
    localStorage.setItem(CHAVE_STORAGE, valor);
  };

  return {
    layoutAtual,
    ehCockpit,
    ehPainelEsportivo,
    selecionarLayout
  };
}

import { computed, ref, type ComputedRef, type Ref } from 'vue';
import { LAYOUT_MONITORAMENTO } from '@/constants/geral-constants';

const CHAVE_STORAGE = 'pmw-monitor-layout';

interface UseLayoutMonitoramentoReturn {
  layoutAtual: Ref<string>;
  ehPainelEsportivo: ComputedRef<boolean>;
  selecionarLayout: (valor: string) => void;
}

const layoutSalvo = localStorage.getItem(CHAVE_STORAGE);
const layoutAtual = ref<string>(
  layoutSalvo === LAYOUT_MONITORAMENTO.PAINEL_ESPORTIVO.valor
    ? layoutSalvo
    : LAYOUT_MONITORAMENTO.PADRAO.valor
);

export function useLayoutMonitoramento(): UseLayoutMonitoramentoReturn {
  const ehPainelEsportivo = computed(
    () => layoutAtual.value === LAYOUT_MONITORAMENTO.PAINEL_ESPORTIVO.valor
  );

  const selecionarLayout = (valor: string): void => {
    layoutAtual.value = valor;
    localStorage.setItem(CHAVE_STORAGE, valor);
  };

  return {
    layoutAtual,
    ehPainelEsportivo,
    selecionarLayout
  };
}

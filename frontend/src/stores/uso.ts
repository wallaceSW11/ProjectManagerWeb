import { defineStore } from 'pinia';
import UsoService from '@/services/usoService';
import type { IRelatorioUso } from '@/types';

interface UsoState {
  gerandoRelatorio: boolean;
  ultimoRelatorio: IRelatorioUso | null;
}

export const useUsoStore = defineStore('uso', {
  state: (): UsoState => ({
    gerandoRelatorio: false,
    ultimoRelatorio: null
  }),

  actions: {
    async gerarRelatorio(): Promise<void> {
      this.gerandoRelatorio = true;
      try {
        this.ultimoRelatorio = await UsoService.gerarRelatorio();
      } finally {
        this.gerandoRelatorio = false;
      }
    }
  }
});

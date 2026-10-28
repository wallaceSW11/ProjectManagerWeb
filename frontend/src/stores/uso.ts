import { defineStore } from 'pinia';
import UsoService from '@/services/usoService';
import type { IRelatorioUso, IUsoDia } from '@/types';

interface UsoState {
  dias: IUsoDia[];
  diaSelecionado: string | null;
  gerandoRelatorio: boolean;
  ultimoRelatorio: IRelatorioUso | null;
}

const dataLocal = (data: Date): string =>
  `${data.getFullYear()}-${String(data.getMonth() + 1).padStart(2, '0')}-${String(data.getDate()).padStart(2, '0')}`;

export const useUsoStore = defineStore('uso', {
  state: (): UsoState => ({
    dias: [],
    diaSelecionado: null,
    gerandoRelatorio: false,
    ultimoRelatorio: null
  }),

  actions: {
    async carregarDias(): Promise<void> {
      try {
        this.dias = await UsoService.listarDias();
      } catch {
        this.dias = [];
      }

      if (this.dias.some(dia => dia.data === this.diaSelecionado)) return;

      const hoje = dataLocal(new Date());
      const temHoje = this.dias.some(dia => dia.data === hoje);
      this.diaSelecionado = temHoje
        ? hoje
        : (this.dias[this.dias.length - 1]?.data ?? null);
    },

    async gerarRelatorio(): Promise<void> {
      this.gerandoRelatorio = true;
      try {
        this.ultimoRelatorio = await UsoService.gerarRelatorio(
          this.diaSelecionado ?? undefined
        );
      } finally {
        this.gerandoRelatorio = false;
      }
    }
  }
});

import { defineStore } from 'pinia';
import MonitoramentoService from '@/services/monitoramentoService';
import type { IMonitoramentoSnapshot } from '@/types';

interface MonitoramentoState {
  snapshot: IMonitoramentoSnapshot | null;
  conectado: boolean;
  ultimaAtualizacao: Date | null;
  erro: string | null;
}

let service: MonitoramentoService | null = null;

export const useMonitoramentoStore = defineStore('monitoramento', {
  state: (): MonitoramentoState => ({
    snapshot: null,
    conectado: false,
    ultimaAtualizacao: null,
    erro: null
  }),

  getters: {
    plataforma: (state): string => state.snapshot?.plataforma ?? '',
    clientesConectados: (state): number =>
      state.snapshot?.clientesConectados ?? 0
  },

  actions: {
    conectar(): void {
      if (service) return;
      service = new MonitoramentoService();

      service.conectar(
        (data: IMonitoramentoSnapshot) => {
          this.snapshot = data;
          this.ultimaAtualizacao = new Date();
          this.erro = null;
        },
        (status: boolean) => {
          this.conectado = status;
          if (!status) this.snapshot = null;
        }
      );
    },

    desconectar(): void {
      service?.desconectar();
      service = null;
      this.conectado = false;
      this.snapshot = null;
    }
  }
});

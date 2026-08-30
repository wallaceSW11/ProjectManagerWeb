import { defineStore } from 'pinia';
import MonitoramentoService from '@/services/monitoramentoService';
import type {
  IMonitoramentoSnapshot,
  IProcessoInfo,
  TipoTopProcessos
} from '@/types';

interface MonitoramentoState {
  snapshot: IMonitoramentoSnapshot | null;
  conectado: boolean;
  ultimaAtualizacao: Date | null;
  erro: string | null;
  processos: Record<TipoTopProcessos, IProcessoInfo[]>;
  carregandoProcessos: boolean;
  erroProcessos: string | null;
}

export const useMonitoramentoStore = defineStore('monitoramento', {
  state: (): MonitoramentoState => ({
    snapshot: null,
    conectado: false,
    ultimaAtualizacao: null,
    erro: null,
    processos: { cpu: [], ram: [] },
    carregandoProcessos: false,
    erroProcessos: null
  }),

  getters: {
    plataforma: (state): string => state.snapshot?.plataforma ?? '',
    clientesConectados: (state): number =>
      state.snapshot?.clientesConectados ?? 0
  },

  actions: {
    conectar(): void {
      if (this.conectado) return;

      MonitoramentoService.conectar(
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
      MonitoramentoService.desconectar();
      this.conectado = false;
      this.snapshot = null;
    },

    async carregarTopProcessos(tipo: TipoTopProcessos): Promise<void> {
      this.carregandoProcessos = true;
      try {
        this.processos[tipo] =
          await MonitoramentoService.obterTopProcessos(tipo);
        this.erroProcessos = null;
      } catch {
        this.erroProcessos = 'Não foi possível carregar os processos.';
      } finally {
        this.carregandoProcessos = false;
      }
    }
  }
});

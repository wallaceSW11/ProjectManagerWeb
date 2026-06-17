import { defineStore } from 'pinia';
import VersaoService from '@/services/VersaoService';

interface VersaoState {
  versaoAtual: string | null;
  versaoNova: string | null;
  urlRelease: string | null;
  urlDownload: string | null;
  carregando: boolean;
  erro: string | null;
}

const INTERVALO_VERIFICACAO = 60 * 60 * 1000;

export const useVersaoStore = defineStore('versao', {
  state: (): VersaoState => ({
    versaoAtual: null,
    versaoNova: null,
    urlRelease: null,
    urlDownload: null,
    carregando: false,
    erro: null
  }),

  getters: {
    temAtualizacao: (state): boolean => state.versaoNova !== null
  },

  actions: {
    async verificarAtualizacao(): Promise<void> {
      this.carregando = true;
      this.erro = null;
      try {
        const response = await VersaoService.obterVersao();
        this.versaoAtual = response.versaoAtual;
        this.versaoNova = response.versaoNova;
        this.urlRelease = response.urlRelease;
        this.urlDownload = response.urlDownload;
      } catch {
        this.erro = 'Erro ao verificar atualização';
      } finally {
        this.carregando = false;
      }
    },

    async carregar(): Promise<void> {
      await this.verificarAtualizacao();
      setInterval(() => this.verificarAtualizacao(), INTERVALO_VERIFICACAO);
    },

    async atualizarAplicacao(): Promise<void> {
      await VersaoService.atualizarAplicacao();
    }
  }
});

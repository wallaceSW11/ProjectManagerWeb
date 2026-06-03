import { defineStore } from 'pinia';
import VersaoService from '@/services/VersaoService';

interface FeaturesState {
  iis: boolean;
  deploy: boolean;
  terminalProfiles: boolean;
  os: string;
  carregado: boolean;
}

export const useFeaturesStore = defineStore('features', {
  state: (): FeaturesState => ({
    iis: true,
    deploy: true,
    terminalProfiles: true,
    os: 'windows',
    carregado: false
  }),

  getters: {
    isWindows: (state): boolean => state.os === 'windows',
    isLinux: (state): boolean => state.os === 'linux',
    pathSeparator: (state): string => (state.os === 'windows' ? '\\' : '/')
  },

  actions: {
    async carregar(): Promise<void> {
      if (this.carregado) return;

      try {
        const response = await VersaoService.obterFeatures();
        this.iis = response.iis;
        this.deploy = response.deploy;
        this.terminalProfiles = response.terminalProfiles;
        this.os = response.os;
        this.carregado = true;
      } catch {
        // Em caso de erro, mantém defaults (Windows) para não quebrar
      }
    }
  }
});

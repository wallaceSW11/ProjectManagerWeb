import { defineStore } from 'pinia';
import SiteIISService from '@/services/SiteIISService';
import SiteIISModel, { type ISiteIIS, type ISiteIISDeployResponse } from '@/models/SiteIISModel';

interface SiteIISState {
  sites: ISiteIISDeployResponse[];
  siteAtual: ISiteIIS | null;
  loading: boolean;
  error: string | null;
}

export const useSiteIISStore = defineStore('siteIIS', {
  state: (): SiteIISState => ({
    sites: [],
    siteAtual: null,
    loading: false,
    error: null
  }),

  getters: {
    getSiteById: (state) => (identificador: string) => {
      return state.sites.find(s => s.identificador === identificador);
    },

    quantidadeSites: (state): number => state.sites.length
  },

  actions: {
    async carregarSites(): Promise<void> {
      this.loading = true;
      this.error = null;
      try {
        this.sites = await SiteIISService.getSites();
      } catch (error: any) {
        this.error = error.message || 'Erro ao carregar sites';
        console.error('Falha ao carregar sites:', error);
        throw error;
      } finally {
        this.loading = false;
      }
    },

    async carregarSite(identificador: string): Promise<void> {
      this.loading = true;
      this.error = null;
      try {
        this.siteAtual = await SiteIISService.getSiteById(identificador);
      } catch (error: any) {
        this.error = error.message || 'Erro ao carregar site';
        console.error('Falha ao carregar site:', error);
        throw error;
      } finally {
        this.loading = false;
      }
    },

    async adicionarSite(site: ISiteIIS): Promise<void> {
      this.loading = true;
      this.error = null;
      try {
        const novoSite = await SiteIISService.adicionarSite(site);
        await this.carregarSites(); // Recarrega lista
      } catch (error: any) {
        this.error = error.message || 'Erro ao adicionar site';
        console.error('Falha ao adicionar site:', error);
        throw error;
      } finally {
        this.loading = false;
      }
    },

    async atualizarSite(site: ISiteIIS): Promise<void> {
      this.loading = true;
      this.error = null;
      try {
        await SiteIISService.atualizarSite(site);
        await this.carregarSites(); // Recarrega lista
      } catch (error: any) {
        this.error = error.message || 'Erro ao atualizar site';
        console.error('Falha ao atualizar site:', error);
        throw error;
      } finally {
        this.loading = false;
      }
    },

    async excluirSite(identificador: string): Promise<void> {
      this.loading = true;
      this.error = null;
      try {
        await SiteIISService.excluirSite(identificador);
        this.sites = this.sites.filter(s => s.identificador !== identificador);
      } catch (error: any) {
        this.error = error.message || 'Erro ao excluir site';
        console.error('Falha ao excluir site:', error);
        throw error;
      } finally {
        this.loading = false;
      }
    },

    async dispararDeploy(identificador: string) {
      this.loading = true;
      this.error = null;
      try {
        const resultado = await SiteIISService.dispararDeploy(identificador);
        return resultado;
      } catch (error: any) {
        this.error = error.message || 'Erro ao disparar deploy';
        console.error('Falha ao disparar deploy:', error);
        throw error;
      } finally {
        this.loading = false;
      }
    }
  }
});

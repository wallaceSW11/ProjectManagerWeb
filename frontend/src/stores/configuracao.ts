import { defineStore } from 'pinia'
import ConfiguracaoService from '../services/ConfiguracaoService'
import ConfiguracaoModel from '../models/ConfiguracaoModel'
import type { IConfiguracao } from '@/types'

interface ConfiguracaoState {
  configuracao: IConfiguracao
}

export const useConfiguracaoStore = defineStore('configuracao', {
  state: (): ConfiguracaoState => ({
    configuracao: new ConfiguracaoModel(),
  }),

  getters: {
    diretorioRaiz: (state): string => state.configuracao.diretorioRaiz,
    perfisVSCode: (state): Array<{ nome: string }> =>
      state.configuracao.perfisVSCode,
  },

  actions: {
    async carregarConfiguracao(): Promise<void> {
      try {
        const response = await ConfiguracaoService.getConfiguracao()
        this.configuracao = new ConfiguracaoModel(response)
      } catch (error) {
        console.error('Falha ao carregar configurações:', error)
      }
    },

    async salvarConfiguracao(novaConfiguracao: IConfiguracao): Promise<void> {
      try {
        await ConfiguracaoService.postConfiguracao(novaConfiguracao)
        this.configuracao = new ConfiguracaoModel(novaConfiguracao)
      } catch (error) {
        console.error('Falha ao salvar configurações:', error)
        throw error
      }
    },
  },
})

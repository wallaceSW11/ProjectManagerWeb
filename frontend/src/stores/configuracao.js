import { defineStore } from 'pinia'
import ConfiguracaoService from '../services/ConfiguracaoService'
import ConfiguracaoModel from '../models/ConfiguracaoModel'

export const useConfiguracaoStore = defineStore('configuracao', {
  state: () => ({
    configuracao: new ConfiguracaoModel()
  }),
  
  getters: {
    diretorioRaiz: (state) => state.configuracao.diretorioRaiz,
    perfisVSCode: (state) => state.configuracao.perfisVSCode
  },
  
  actions: {
    async carregarConfiguracao() {
      try {
        const response = await ConfiguracaoService.getConfiguracao()
        this.configuracao = new ConfiguracaoModel(response)
      } catch (error) {
        console.error('Falha ao carregar configurações:', error)
      }
    },

    async salvarConfiguracao(novaConfiguracao) {
      try {
        await ConfiguracaoService.postConfiguracao(novaConfiguracao)
        this.configuracao = new ConfiguracaoModel(novaConfiguracao)
      } catch (error) {
        console.error('Falha ao salvar configurações:', error)
        throw error // Propaga o erro para tratamento no componente se necessário
      }
    }
  }
})
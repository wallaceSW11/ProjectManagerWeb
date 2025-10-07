import BaseApiService from './BaseApiService'
import type { IConfiguracao } from '@/types'

class ConfiguracaoService extends BaseApiService {
  async getConfiguracao(): Promise<IConfiguracao> {
    return await this.get<IConfiguracao>('configuracoes')
  }

  async postConfiguracao(configuracao: IConfiguracao): Promise<void> {
    return await this.post('configuracoes', configuracao)
  }
}

export default new ConfiguracaoService()
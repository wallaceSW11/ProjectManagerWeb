import BaseApiService from './BaseApiService';
import type { IConfiguracao } from '@/types';

class ConfiguracaoService extends BaseApiService {
  async getConfiguracao(): Promise<IConfiguracao> {
    return await this.get<IConfiguracao>('configuracoes');
  }

  async postConfiguracao(configuracao: IConfiguracao): Promise<void> {
    return await this.post('configuracoes', configuracao);
  }

  async renomearPerfil(nomeAntigo: string, nomeNovo: string): Promise<void> {
    return await this.put(`configuracoes/perfis/${encodeURIComponent(nomeAntigo)}`, nomeNovo);
  }
}

export default new ConfiguracaoService();

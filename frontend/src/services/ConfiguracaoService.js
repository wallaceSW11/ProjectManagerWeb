import BaseApiService from './BaseApiService';

class ConfiguracaoService extends BaseApiService {
    async getConfiguracao() {
        return await this.get('configuracoes');
    }
}

export default new ConfiguracaoService();
import BaseApiService from './BaseApiService';

class ConfiguracaoService extends BaseApiService {
    async getConfiguracao() {
        return await this.get('configuracoes');
    }

    async postConfiguracao(configuracao) {
        return await this.post('configuracoes', configuracao);
    }
}

export default new ConfiguracaoService();
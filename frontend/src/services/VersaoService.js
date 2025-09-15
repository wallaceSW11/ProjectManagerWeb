import BaseApiService from './BaseApiService';

class VersaoService extends BaseApiService {
    async obterVersao() {
        return await this.get('versao');
    }
}

export default new VersaoService();
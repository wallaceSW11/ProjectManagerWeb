import BaseApiService from './BaseApiService';

class PastasService extends BaseApiService {
    async getPastas() {
        return await this.get('pastas');
    }

    async criar(pasta) {
        return await this.post('pastas', pasta);
    }

    async atualizarIndices(indices) {
        return await this.put('pastas/indices', indices);
    }
}

export default new PastasService();
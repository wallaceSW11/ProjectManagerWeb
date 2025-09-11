import BaseApiService from './BaseApiService';

class PastasService extends BaseApiService {
    async getPastas() {
        return await this.get('pastas');
    }
}

export default new PastasService();
import BaseApiService from './BaseApiService';

class RepositoriosService extends BaseApiService {
    async getRepositorios() {
        return await this.get('repositorios');
    }
}

export default new RepositoriosService();
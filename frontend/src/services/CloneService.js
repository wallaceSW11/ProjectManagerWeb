import BaseApiService from './BaseApiService';

class CloneService extends BaseApiService {
    async clonar(clone) {
        return await this.post('clones', clone);
    }
}

export default new CloneService();
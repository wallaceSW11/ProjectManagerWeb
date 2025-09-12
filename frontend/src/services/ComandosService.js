import BaseApiService from './BaseApiService';

class ComandosService extends BaseApiService {
    async executarComando(comandos) {
        return await this.post('comandos', comandos );
    }
}

export default new ComandosService();
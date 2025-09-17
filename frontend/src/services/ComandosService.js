import BaseApiService from "./BaseApiService";

class ComandosService extends BaseApiService {
  async executarComando(comandos) {
    return await this.post("comandos", comandos);
  }

  async executarComandoMenu(comando) {
    return await this.post("comandos/menu", comando);
  }
}

export default new ComandosService();

import BaseApiService from "./BaseApiService";

class RepositoriosService extends BaseApiService {
  async getRepositorios() {
    return await this.get("repositorios");
  }

  async adicionarRepositorio(repositorio) {
    return await this.post("repositorios", repositorio);
  }
  
  async atualizarRepositorio(repositorio) {
    return await this.put(`repositorios/${repositorio.identificador}`, repositorio);
  }

  async excluirRepositorio(repositorio) {
    return await this.delete(`repositorios/${repositorio.identificador}`);
  }
}

export default new RepositoriosService();

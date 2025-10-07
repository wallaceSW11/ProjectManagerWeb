import BaseApiService from './BaseApiService';
import type { IRepositorio } from '@/types';

class RepositoriosService extends BaseApiService {
  async getRepositorios(): Promise<IRepositorio[]> {
    return await this.get<IRepositorio[]>('repositorios');
  }

  async adicionarRepositorio(repositorio: IRepositorio): Promise<void> {
    return await this.post('repositorios', repositorio);
  }

  async atualizarRepositorio(repositorio: IRepositorio): Promise<void> {
    return await this.put(
      `repositorios/${repositorio.identificador}`,
      repositorio
    );
  }

  async excluirRepositorio(repositorio: IRepositorio): Promise<void> {
    return await this.delete(`repositorios/${repositorio.identificador}`);
  }
}

export default new RepositoriosService();

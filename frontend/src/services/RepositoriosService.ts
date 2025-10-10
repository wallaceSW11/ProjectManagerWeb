import BaseApiService from './BaseApiService';
import type { IRepositorio } from '@/types';

class RepositoriosService extends BaseApiService {
  async getRepositorios(): Promise<IRepositorio[]> {
    return await this.get<IRepositorio[]>('repositorios');
  }

  async adicionarRepositorio(repositorio: IRepositorio): Promise<void> {
    const dto = (repositorio as any).toDTO ? (repositorio as any).toDTO() : repositorio;
    return await this.post('repositorios', dto);
  }

  async atualizarRepositorio(repositorio: IRepositorio): Promise<void> {
    const dto = (repositorio as any).toDTO ? (repositorio as any).toDTO() : repositorio;
    return await this.put(
      `repositorios/${repositorio.identificador}`,
      dto
    );
  }

  async excluirRepositorio(repositorio: IRepositorio): Promise<void> {
    return await this.delete(`repositorios/${repositorio.identificador}`);
  }
}

export default new RepositoriosService();

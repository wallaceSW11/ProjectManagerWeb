import BaseApiService from './BaseApiService';
import type { IRepositorio, ICodigoTarefa } from '@/types';

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

  async atualizarOrdem(indices: { identificador: string; indice: number }[]): Promise<void> {
    return await this.put('repositorios/indices', indices);
  }

  async adicionarCodigoTarefa(repositorioId: string, codigoTarefa: ICodigoTarefa): Promise<void> {
    return await this.post(`repositorios/${repositorioId}/codigos-tarefa`, codigoTarefa);
  }

  async atualizarCodigoTarefa(repositorioId: string, codigoId: string, codigoTarefa: ICodigoTarefa): Promise<void> {
    return await this.put(`repositorios/${repositorioId}/codigos-tarefa/${codigoId}`, codigoTarefa);
  }

  async removerCodigoTarefa(repositorioId: string, codigoId: string): Promise<void> {
    return await this.delete(`repositorios/${repositorioId}/codigos-tarefa/${codigoId}`);
  }

  async buscarCodigoTarefa(iniciais: string): Promise<{ codigoTarefa: ICodigoTarefa; repositorio: any } | null> {
    try {
      return await this.get(`repositorios/codigos-tarefa/${encodeURIComponent(iniciais)}`);
    } catch {
      return null;
    }
  }
}

export default new RepositoriosService();

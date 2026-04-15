import BaseApiService from './BaseApiService';
import type { IPasta } from '@/types';

interface IndiceAtualizar {
  identificador: string;
  indice: number;
}

interface ExpandidoAtualizar {
  pastaId: string;
  projetoId: string;
  expandido: boolean;
}

class PastasService extends BaseApiService {
  async getPastas(): Promise<IPasta[]> {
    return await this.get<IPasta[]>('pastas');
  }

  async criar(pasta: IPasta): Promise<void> {
    return await this.post('pastas', pasta);
  }

  async atualizarIndices(indices: IndiceAtualizar[]): Promise<void> {
    return await this.put('pastas/indices', indices);
  }

  async atualizarExpandido(payload: ExpandidoAtualizar): Promise<void> {
    return await this.patch(`pastas/projetos/expandido`, payload);
  }

  async getOcultas(): Promise<string[]> {
    return await this.get<string[]>('pastas/ocultas');
  }

  async ocultar(diretorio: string): Promise<void> {
    return await this.post('pastas/ocultar', { diretorio });
  }

  async restaurar(diretorio: string): Promise<void> {
    return await this.post('pastas/restaurar', { diretorio });
  }
}

export default new PastasService();

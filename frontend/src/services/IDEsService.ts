import BaseApiService from './BaseApiService';
import type { IIDE } from '@/types';

class IDEsService extends BaseApiService {
  async getIDEs(): Promise<IIDE[]> {
    return await this.get<IIDE[]>('ides');
  }

  async adicionarIDE(ide: IIDE): Promise<void> {
    const dto = (ide as any).toDTO ? (ide as any).toDTO() : ide;
    return await this.post('ides', dto);
  }

  async atualizarIDE(ide: IIDE): Promise<void> {
    const dto = (ide as any).toDTO ? (ide as any).toDTO() : ide;
    return await this.put(`ides/${ide.identificador}`, dto);
  }

  async excluirIDE(ide: IIDE): Promise<void> {
    return await this.delete(`ides/${ide.identificador}`);
  }
}

export default new IDEsService();

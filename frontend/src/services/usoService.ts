import BaseApiService from './BaseApiService';
import type { IRelatorioUso } from '@/types';

class UsoService extends BaseApiService {
  async gerarRelatorio(): Promise<IRelatorioUso> {
    return await this.post<IRelatorioUso>('uso/relatorio', {});
  }
}

export default new UsoService();

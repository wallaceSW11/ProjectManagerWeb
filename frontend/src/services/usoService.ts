import BaseApiService from './BaseApiService';
import type { IRelatorioUso, IUsoDia } from '@/types';

class UsoService extends BaseApiService {
  async listarDias(): Promise<IUsoDia[]> {
    return await this.get<IUsoDia[]>('uso/dias');
  }

  async gerarRelatorio(data?: string): Promise<IRelatorioUso> {
    return await this.post<IRelatorioUso>('uso/relatorio', { data });
  }
}

export default new UsoService();

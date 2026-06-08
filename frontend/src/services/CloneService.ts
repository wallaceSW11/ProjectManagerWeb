import BaseApiService from './BaseApiService';
import type { IClone } from '@/types';

class CloneService extends BaseApiService {
  async clonar(clone: IClone): Promise<void> {
    return await this.post('clones', clone);
  }

  async verificarBranch(
    url: string,
    branch: string,
    caminhoChaveSSH?: string | null
  ): Promise<{ existe: boolean; erro?: string }> {
    var query = `clones/verificar-branch?url=${encodeURIComponent(url)}&branch=${encodeURIComponent(branch)}`;
    if (caminhoChaveSSH)
      query += `&caminhoChaveSSH=${encodeURIComponent(caminhoChaveSSH)}`;
    return await this.get<{ existe: boolean; erro?: string }>(query);
  }
}

export default new CloneService();

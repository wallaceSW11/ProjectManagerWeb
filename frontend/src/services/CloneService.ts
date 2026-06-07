import BaseApiService from './BaseApiService';
import type { IClone } from '@/types';

class CloneService extends BaseApiService {
  async clonar(clone: IClone): Promise<void> {
    return await this.post('clones', clone);
  }

  async verificarBranch(
    url: string,
    branch: string
  ): Promise<{ existe: boolean; erro?: string }> {
    return await this.get<{ existe: boolean; erro?: string }>(
      `clones/verificar-branch?url=${encodeURIComponent(url)}&branch=${encodeURIComponent(branch)}`
    );
  }
}

export default new CloneService();

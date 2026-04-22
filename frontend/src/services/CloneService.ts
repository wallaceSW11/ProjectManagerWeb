import BaseApiService from './BaseApiService';
import type { IClone } from '@/types';

class CloneService extends BaseApiService {
  async clonar(clone: IClone): Promise<void> {
    return await this.post('clones', clone);
  }

  async verificarBranch(url: string, branch: string): Promise<boolean> {
    const response = await this.get<{ existe: boolean }>(`clones/verificar-branch?url=${encodeURIComponent(url)}&branch=${encodeURIComponent(branch)}`);
    return response.existe;
  }
}

export default new CloneService();

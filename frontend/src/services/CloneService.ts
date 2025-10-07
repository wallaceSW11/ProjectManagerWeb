import BaseApiService from './BaseApiService'
import type { IClone } from '@/types'

class CloneService extends BaseApiService {
  async clonar(clone: IClone): Promise<void> {
    return await this.post('clones', clone)
  }
}

export default new CloneService()
import BaseApiService from './BaseApiService'
import type { IPasta } from '@/types'

interface IndiceAtualizar {
  identificador: string
  indice: number
}

class PastasService extends BaseApiService {
  async getPastas(): Promise<IPasta[]> {
    return await this.get<IPasta[]>('pastas')
  }

  async criar(pasta: IPasta): Promise<void> {
    return await this.post('pastas', pasta)
  }

  async atualizarIndices(indices: IndiceAtualizar[]): Promise<void> {
    return await this.put('pastas/indices', indices)
  }
}

export default new PastasService()
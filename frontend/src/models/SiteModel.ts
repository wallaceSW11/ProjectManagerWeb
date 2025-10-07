import type { ISite } from '@/types'

export default class SiteModel implements ISite {
  nome: string
  porta: string
  status: string

  constructor(data: Partial<ISite> = {}) {
    this.nome = data.nome || ''
    this.porta = data.porta || ''
    this.status = data.status || ''
  }

  static fromJson(json: Partial<ISite>): SiteModel {
    return new SiteModel(json)
  }

  toJson(): ISite {
    return {
      nome: this.nome,
      porta: this.porta,
      status: this.status,
    }
  }
}

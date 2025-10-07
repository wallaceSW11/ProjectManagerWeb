import BaseApiService from './BaseApiService'
import type { ISite } from '@/types'

class IISService extends BaseApiService {
  async getSites(): Promise<ISite[]> {
    return await this.get<ISite[]>('iis/sites')
  }

  async iniciarSite(nomeSite: string): Promise<void> {
    return await this.post(`iis/sites/${nomeSite}/iniciar`, {})
  }

  async pararSite(nomeSite: string): Promise<void> {
    return await this.post(`iis/sites/${nomeSite}/parar`, {})
  }

  async reiniciarSite(nomeSite: string): Promise<void> {
    return await this.post(`iis/sites/${nomeSite}/reiniciar`, {})
  }
}

export default new IISService()

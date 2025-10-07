import BaseApiService from './BaseApiService'

class VersaoService extends BaseApiService {
  async obterVersao(): Promise<string> {
    return await this.get<string>('versao')
  }
}

export default new VersaoService()

import BaseApiService from './BaseApiService';

export interface FeaturesResponse {
  iis: boolean;
  deploy: boolean;
  terminalProfiles: boolean;
  os: string;
}

class VersaoService extends BaseApiService {
  async obterVersao(): Promise<string> {
    return await this.get<string>('versao');
  }

  async obterFeatures(): Promise<FeaturesResponse> {
    return await this.get<FeaturesResponse>('versao/features');
  }
}

export default new VersaoService();

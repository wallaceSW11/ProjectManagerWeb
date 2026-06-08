import BaseApiService from './BaseApiService';

export interface VersaoResponse {
  versaoAtual: string;
  versaoNova: string | null;
  urlRelease: string | null;
  urlDownload: string | null;
}

export interface FeaturesResponse {
  iis: boolean;
  deploy: boolean;
  terminalProfiles: boolean;
  os: string;
}

class VersaoService extends BaseApiService {
  async obterVersao(): Promise<VersaoResponse> {
    return await this.get<VersaoResponse>('versao');
  }

  async obterCompilacao(): Promise<string> {
    return await this.get<string>('versao/compilacao');
  }

  async obterFeatures(): Promise<FeaturesResponse> {
    return await this.get<FeaturesResponse>('versao/features');
  }
}

export default new VersaoService();

import BaseApiService from './BaseApiService';

class TerminalService extends BaseApiService {
  async getPerfis(): Promise<string[]> {
    return await this.get<string[]>('terminal/perfis');
  }
}

export default new TerminalService();

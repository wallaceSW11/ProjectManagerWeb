import BaseApiService from './BaseApiService';

export interface MensagemChat {
  role: 'user' | 'assistant';
  conteudo: string;
}

export interface JarvasResponse {
  resposta: string;
  acoesExecutadas?: string[] | null;
  sucesso: boolean;
  erroDetalhes?: string | null;
}

class JarvasService extends BaseApiService {
  async chat(mensagem: string, historico: MensagemChat[]): Promise<JarvasResponse> {
    return await this.post('jarvas/chat', { mensagem, historico });
  }

  async verificarStatus(): Promise<boolean> {
    try {
      return await this.get<boolean>('jarvas/status');
    } catch {
      return false;
    }
  }
}

export default new JarvasService();

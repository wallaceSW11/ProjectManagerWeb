import type { IConfiguracao } from '@/types';

export default class ConfiguracaoModel implements IConfiguracao {
  diretorioRaiz: string;
  perfisVSCode: Array<{ nome: string }>;

  constructor(obj: Partial<IConfiguracao> = {}) {
    this.diretorioRaiz = obj.diretorioRaiz || '';
    this.perfisVSCode = obj.perfisVSCode || [];
  }
}

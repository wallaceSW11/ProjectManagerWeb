import type { IPerfilMarcacaoProjeto } from '@/types';

export default class PerfilMarcacaoProjetoModel implements IPerfilMarcacaoProjeto {
  identificadorProjeto: string;
  comandos: string[];

  constructor(obj: Partial<IPerfilMarcacaoProjeto> = {}) {
    this.identificadorProjeto = obj.identificadorProjeto || '';
    this.comandos = obj.comandos || [];
  }
}

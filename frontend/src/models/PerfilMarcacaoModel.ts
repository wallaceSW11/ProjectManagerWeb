import type { IPerfilMarcacao } from '@/types';
import PerfilMarcacaoProjetoModel from './PerfilMarcacaoProjetoModel';

export default class PerfilMarcacaoModel implements IPerfilMarcacao {
  identificador: string;
  nome: string;
  projetos: PerfilMarcacaoProjetoModel[];
  abrirIDE: boolean;
  abrirCLI: boolean;

  constructor(obj: Partial<IPerfilMarcacao> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.nome = obj.nome || '';
    this.projetos =
      obj.projetos?.map(p => new PerfilMarcacaoProjetoModel(p)) || [];
    this.abrirIDE = obj.abrirIDE || false;
    this.abrirCLI = obj.abrirCLI || false;
  }
}

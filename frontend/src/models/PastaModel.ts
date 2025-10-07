import type { IPasta, IProjeto, IMenu } from '@/types';
import ProjetoModel from './ProjetoModel';

export default class PastaModel implements IPasta {
  identificador: string;
  diretorio: string;
  codigo: string;
  descricao: string;
  tipo: string;
  branch: string;
  git: string;
  repositorioId?: string;
  projetos: IProjeto[];
  menus: IMenu[];
  index: number;

  constructor(obj: Partial<IPasta> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.diretorio = obj.diretorio || '';
    this.codigo = obj.codigo || '';
    this.descricao = obj.descricao || '';
    this.tipo = obj.tipo || '';
    this.branch = obj.branch || '';
    this.git = obj.git || '';
    this.repositorioId = obj.repositorioId;
    this.projetos = (obj.projetos || []).map((p: any) => new ProjetoModel(p));
    this.menus = obj.menus || [];
    this.index = obj.index || 999;
  }
}

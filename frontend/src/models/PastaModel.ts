import type { IPasta } from '@/types';
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
  cor?: string | null;
  projetos: any[];
  menus: any[];
  index: number;
  ideIdentificador?: string | null;
  nomeRepositorio?: string | null;
  perfilVSCode?: string | null;

  constructor(obj: Partial<IPasta> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.diretorio = obj.diretorio || '';
    this.codigo = obj.codigo || '';
    this.descricao = obj.descricao || '';
    this.tipo = obj.tipo || '';
    this.branch = obj.branch || '';
    this.git = obj.git || '';
    this.repositorioId = obj.repositorioId;
    this.cor = obj.cor;
    this.projetos = obj.projetos?.map(p => new ProjetoModel(p)) || [];
    this.menus = obj.menus || [];
    this.index = obj.index || 0;
    this.ideIdentificador = obj.ideIdentificador || null;
    this.nomeRepositorio = obj.nomeRepositorio || null;
    this.perfilVSCode = obj.perfilVSCode || null;
  }
}

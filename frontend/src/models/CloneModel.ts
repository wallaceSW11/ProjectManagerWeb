import type { IClone, IRepositorio } from '@/types';
import RepositorioModel from './RepositorioModel';

export default class CloneModel implements IClone {
  diretorioRaiz: string;
  codigo: string;
  descricao: string;
  tipo: 'nenhum' | 'feature' | 'bug' | 'hotfix';
  branch: string;
  repositorio: IRepositorio;
  criarBranchRemoto: boolean;
  baixarAgregados: boolean;

  constructor(obj: Partial<IClone> = {}) {
    this.diretorioRaiz = obj.diretorioRaiz || '';
    this.codigo = obj.codigo || '';
    this.descricao = obj.descricao || '';
    this.tipo = obj.tipo || 'nenhum';
    this.branch = obj.branch || '';
    this.repositorio = new RepositorioModel(obj.repositorio);
    this.criarBranchRemoto = obj.criarBranchRemoto || false;
    this.baixarAgregados = obj.baixarAgregados || false;
  }
}

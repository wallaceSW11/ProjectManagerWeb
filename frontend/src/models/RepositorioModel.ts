import type { IRepositorio, IProjeto, IMenu } from '@/types';
import MenuModel from './MenuModel';
import ProjetoModel from './ProjetoModel';

export default class RepositorioModel implements IRepositorio {
  identificador: string;
  url: string | null;
  titulo: string;
  nome: string;
  projetos: IProjeto[];
  agregados: string[];
  menus: IMenu[];

  constructor(obj: Partial<IRepositorio> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.url = obj.url || null;
    this.titulo = obj.titulo || '';
    this.nome = obj.nome || '';
    this.projetos = Array.isArray(obj.projetos)
      ? obj.projetos.map((p: any) => new ProjetoModel(p))
      : [];
    this.agregados = obj.agregados || [];
    this.menus = obj.menus?.map((m: any) => new MenuModel(m)) || [];
  }
}

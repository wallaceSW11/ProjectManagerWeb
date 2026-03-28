import type { IRepositorio, IProjeto, IMenu } from '@/types';
import MenuModel from './MenuModel';
import ProjetoModel from './ProjetoModel';

export default class RepositorioModel implements IRepositorio {
  identificador: string;
  url: string | null;
  titulo: string;
  nome: string;
  cor?: string | null;
  projetos: IProjeto[];
  agregados: string[];
  menus: IMenu[];
  ideIdentificador?: string | null;
  perfilVSCode?: string | null;
  comandoClone?: string | null;

  constructor(obj: Partial<IRepositorio> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.url = obj.url || null;
    this.titulo = obj.titulo || '';
    this.nome = obj.nome || '';
    this.cor = obj.cor || '#1976d2';
    this.projetos = Array.isArray(obj.projetos)
      ? obj.projetos.map((p: any) => new ProjetoModel(p))
      : [];
    this.agregados = obj.agregados || [];
    this.menus = obj.menus?.map((m: any) => new MenuModel(m)) || [];
    this.ideIdentificador = obj.ideIdentificador || null;
    this.perfilVSCode = obj.perfilVSCode || null;
    this.comandoClone = obj.comandoClone || null;
  }

  toDTO() {
    return {
      identificador: this.identificador,
      url: this.url,
      titulo: this.titulo,
      nome: this.nome,
      cor: this.cor,
      projetos: this.projetos.map((p: any) => p.toDTO()),
      agregados: this.agregados,
      menus: this.menus,
      ideIdentificador: this.ideIdentificador,
      perfilVSCode: this.perfilVSCode,
      comandoClone: this.comandoClone
    };
  }
}

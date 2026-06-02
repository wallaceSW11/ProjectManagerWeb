import type { IRepositorio, IProjeto, IMenu, IPerfilMarcacao, ICodigoTarefa } from '@/types';
import MenuModel from './MenuModel';
import ProjetoModel from './ProjetoModel';
import PerfilMarcacaoModel from './PerfilMarcacaoModel';

export default class RepositorioModel implements IRepositorio {
  identificador: string;
  url: string | null;
  titulo: string;
  nome: string;
  cor?: string | null;
  projetos: IProjeto[];
  agregados: string[];
  menus: IMenu[];
  perfis: IPerfilMarcacao[];
  codigosTarefa: ICodigoTarefa[];
  ideIdentificador?: string | null;
  perfilVSCode?: string | null;
  subdiretorio?: string | null;
  cliComando?: string | null;
  perfilTerminal?: string | null;
  abrirWorkspace: boolean;
  cliComandoComplementar?: string | null;
  branchBase?: string | null;
  urlBaseGestorTarefas?: string | null;
  pastaCentralizadora?: string | null;

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
    this.perfis = obj.perfis?.map((p: any) => new PerfilMarcacaoModel(p)) || [];
    this.codigosTarefa = obj.codigosTarefa || [];
    this.ideIdentificador = obj.ideIdentificador || null;
    this.perfilVSCode = obj.perfilVSCode || null;
    this.subdiretorio = obj.subdiretorio || null;
    this.cliComando = obj.cliComando || null;
    this.perfilTerminal = obj.perfilTerminal || null;
    this.abrirWorkspace = obj.abrirWorkspace !== undefined ? obj.abrirWorkspace : true;
    this.cliComandoComplementar = obj.cliComandoComplementar || null;
    this.branchBase = obj.branchBase || null;
    this.urlBaseGestorTarefas = obj.urlBaseGestorTarefas || null;
    this.pastaCentralizadora = obj.pastaCentralizadora || null;
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
      perfis: this.perfis,
      codigosTarefa: this.codigosTarefa,
      ideIdentificador: this.ideIdentificador,
      perfilVSCode: this.perfilVSCode,
      subdiretorio: this.subdiretorio,
      cliComando: this.cliComando,
      perfilTerminal: this.perfilTerminal,
      abrirWorkspace: this.abrirWorkspace,
      cliComandoComplementar: this.cliComandoComplementar,
      branchBase: this.branchBase,
      urlBaseGestorTarefas: this.urlBaseGestorTarefas,
      pastaCentralizadora: this.pastaCentralizadora
    };
  }
}

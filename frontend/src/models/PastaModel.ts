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
  nomeIDE?: string | null;
  nomeRepositorio?: string | null;
  perfilVSCode?: string | null;
  subdiretorio?: string | null;
  cliComando?: string | null;
  perfilTerminal?: string | null;
  abrirWorkspace: boolean;
  fixada: boolean;
  ordemFixada: number;
  cliComandoComplementar?: string | null;
  nomeCli?: string | null;
  nomeAba?: string | null;

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
    this.nomeIDE = obj.nomeIDE || null;
    this.nomeRepositorio = obj.nomeRepositorio || null;
    this.perfilVSCode = obj.perfilVSCode || null;
    this.subdiretorio = obj.subdiretorio || null;
    this.cliComando = obj.cliComando || null;
    this.perfilTerminal = obj.perfilTerminal || null;
    this.abrirWorkspace = obj.abrirWorkspace !== undefined ? obj.abrirWorkspace : true;
    this.fixada = obj.fixada || false;
    this.ordemFixada = obj.ordemFixada || 0;
    this.cliComandoComplementar = obj.cliComandoComplementar || null;
    this.nomeCli = obj.nomeCli || null;
    this.nomeAba = obj.nomeAba || null;
  }
}

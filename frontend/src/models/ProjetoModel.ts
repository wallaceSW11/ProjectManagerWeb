import type { IProjeto, IComandos } from '@/types';

class ComandosModel implements IComandos {
  instalar: string | null;
  iniciar: string | null;
  buildar: string | null;
  abrirNoVSCode: boolean;

  constructor(obj: Partial<IComandos> = {}) {
    this.instalar = obj.instalar || null;
    this.iniciar = obj.iniciar || null;
    this.buildar = obj.buildar || null;
    this.abrirNoVSCode = !!obj.abrirNoVSCode;
  }
}

export default class ProjetoModel implements IProjeto {
  identificador: string;
  nome: string;
  nomeRepositorio: string;
  subdiretorio: string;
  perfilVSCode: string;
  comandos: IComandos;
  arquivoCoverage: string;
  expandido: boolean;

  constructor(obj: Partial<IProjeto> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.nome = obj.nome || '';
    this.nomeRepositorio = obj.nomeRepositorio || '';
    this.subdiretorio = obj.subdiretorio || '';
    this.perfilVSCode = obj.perfilVSCode || '';
    this.comandos = new ComandosModel(obj.comandos);
    this.arquivoCoverage = obj.arquivoCoverage || '';
    this.expandido = false;
  }
}

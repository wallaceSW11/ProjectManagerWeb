export interface ISiteIIS {
  identificador: string;
  titulo: string;
  nome: string;
  pastaRaiz: string;
  pastas: IPastaDeploy[];
  poolsAplicacao: string[];
}

export interface ISiteIISDeployResponse {
  identificador: string;
  titulo: string;
  nome: string;
  pastaRaiz: string;
  quantidadePastas: number;
  quantidadePools: number;
}

export interface IPastaDeploy {
  identificador: string;
  diretorioTrabalho: string;
  comandoPublish: string;
  caminhoOrigem: string;
  caminhoDestino: string;
  nomePastaDestino: string;
}

export interface IAtualizarSiteResponse {
  sucesso: boolean;
  mensagem: string;
  logCompleto: string[];
}

export default class SiteIISModel implements ISiteIIS {
  identificador: string;
  titulo: string;
  nome: string;
  pastaRaiz: string;
  pastas: IPastaDeploy[];
  poolsAplicacao: string[];

  constructor(obj: Partial<ISiteIIS> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.titulo = obj.titulo || '';
    this.nome = obj.nome || '';
    this.pastaRaiz = obj.pastaRaiz || '';
    this.pastas = obj.pastas || [];
    this.poolsAplicacao = obj.poolsAplicacao || [];
  }

  static fromJson(json: Partial<ISiteIIS>): SiteIISModel {
    return new SiteIISModel(json);
  }

  toDTO() {
    return {
      identificador: this.identificador,
      titulo: this.titulo,
      nome: this.nome,
      pastaRaiz: this.pastaRaiz,
      pastas: this.pastas,
      poolsAplicacao: this.poolsAplicacao
    };
  }
}

export class PastaDeployModel implements IPastaDeploy {
  identificador: string;
  diretorioTrabalho: string;
  comandoPublish: string;
  caminhoOrigem: string;
  caminhoDestino: string;
  nomePastaDestino: string;

  constructor(obj: Partial<IPastaDeploy> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.diretorioTrabalho = obj.diretorioTrabalho || '';
    this.comandoPublish = obj.comandoPublish || '';
    this.caminhoOrigem = obj.caminhoOrigem || '';
    this.caminhoDestino = obj.caminhoDestino || '';
    this.nomePastaDestino = obj.nomePastaDestino || '';
  }

  static fromJson(json: Partial<IPastaDeploy>): PastaDeployModel {
    return new PastaDeployModel(json);
  }

  toDTO() {
    return {
      identificador: this.identificador,
      diretorioTrabalho: this.diretorioTrabalho,
      comandoPublish: this.comandoPublish,
      caminhoOrigem: this.caminhoOrigem,
      caminhoDestino: this.caminhoDestino,
      nomePastaDestino: this.nomePastaDestino
    };
  }
}

export default class ProjetoModel {
  constructor(obj) {
    obj = obj || {};

    this.identificador = obj.identificador || crypto.randomUUID();
    this.nome = obj.nome || null;
    this.nomeRepositorio = obj.nomeRepositorio || null;
    this.subdiretorio = obj.subdiretorio || null;
    this.perfilVSCode = obj.perfilVSCode || null;
    this.comandos = new ComandosModel(obj.comandos);
    this.arquivoCoverage = obj.arquivoCoverage || null;
  }
}

class ComandosModel {
  constructor(obj) {
    obj = obj || {};

    this.instalar = obj.instalar || null;
    this.iniciar = obj.iniciar || null;
    this.buildar = obj.buildar || null;
    this.abrirNoVSCode = !!obj.abrirNoVSCode;
  }
}
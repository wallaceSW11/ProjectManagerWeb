export default class ProjetoModel {
  constructor(obj) {
    obj = obj || {};

    this.identificador = obj.identificador || crypto.randomUUID();
    this.nome = obj.nome || "";
    this.nomeRepositorio = obj.nomeRepositorio || "";
    this.subdiretorio = obj.subdiretorio || "";
    this.perfilVSCode = obj.perfilVSCode || "";
    this.comandos = new ComandosModel(obj.comandos);
    this.arquivoCoverage = obj.arquivoCoverage || "";
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
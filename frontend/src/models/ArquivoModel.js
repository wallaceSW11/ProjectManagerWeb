export default class ArquivoModel {
  constructor(obj) {
    obj = obj || {};

    this.identificador = obj.identificador || crypto.randomUUID();
    this.arquivo = obj.arquivo;
    this.destino = obj.destino;
    this.ignorarGit = obj.ignorarGit;
  }
}
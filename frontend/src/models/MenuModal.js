import ArquivoModel from "./ArquivoModel";

export default class MenuModel {
  constructor(obj) {
    obj = obj || {};

    this.identificador = obj.identificador || crypto.randomUUID();
    this.titulo = obj.titulo;
    this.tipo = obj.tipo;
    this.arquivos = obj.arquivos?.map(a => new ArquivoModel(a)) || [];
  }
}
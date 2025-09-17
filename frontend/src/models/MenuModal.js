import ArquivoModel from "./ArquivoModel";

export default class MenuModel {
  constructor(obj) {
    obj = obj || {};

    this.id = obj.id || crypto.randomUUID();
    this.titulo = obj.titulo;
    this.tipo = obj.tipo;
    this.arquivos = obj.arquivos?.map(a => new ArquivoModel(a)) || [];
  }
}
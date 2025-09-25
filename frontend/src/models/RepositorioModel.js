import MenuModel from "./MenuModel";
import ProjetoModel from "./ProjetoModel";

export default class RepositorioModel {
  constructor(obj) {
    obj = obj || {};

    this.identificador = obj.identificador || crypto.randomUUID();
    this.url = obj.url || null;
    this.titulo = obj.titulo || "";
    this.nome = obj.nome || "";
    this.projetos = Array.isArray(obj.projetos)
      ? obj.projetos.map(p => new ProjetoModel(p))
      : [];
    this.agregados = obj.agregados || [];
    this.menus = obj.menus?.map(m => new MenuModel(m)) || [];
  }
}
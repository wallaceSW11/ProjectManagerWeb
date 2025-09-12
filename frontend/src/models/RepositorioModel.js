import ProjetoModel from "./ProjetoModel";

export default class RepositorioModel {
  constructor(obj) {
    obj = obj || {};

    this.id = obj.id;
    this.url = obj.url || null;
    this.nome = obj.nome || null;
    this.projetos = Array.isArray(obj.projetos)
      ? obj.projetos.map(p => new ProjetoModel(p))
      : [];
    this.agregados = obj.agregados || null;
  }
}
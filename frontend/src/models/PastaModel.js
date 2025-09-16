import ProjetoModel from "@/models/ProjetoModel.js";

export default class PastaModel {
  constructor(obj) {
    obj = obj || {};

    this.id = obj.id;
    this.diretorio = obj.diretorio || "";
    this.codigo = obj.codigo || "";
    this.descricao = obj.descricao || "";
    this.tipo = obj.tipo || "";
    this.branch = obj.branch || "";
    this.git = obj.git || "";
    this.repositorioId = obj.repositorioId;
    this.projetos = (obj.projetos || []).map((p) => new ProjetoModel(p));
  }
}

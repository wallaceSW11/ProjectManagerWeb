import RepositorioModel from "./RepositorioModel";

export default class CloneModel {
  constructor(obj) {
    obj = obj || {};

    this.diretorioRaiz = obj.diretorioRaiz;
    this.codigo = obj.codigo;
    this.descricao = obj.descricao;
    this.tipo = obj.tipo || "nenhum";
    this.branch = obj.branch;
    this.repositorio = new RepositorioModel(obj.repositorio);
    this.criarBranchRemoto = obj.criarBranchRemoto;
    this.baixarAgregados = obj.baixarAgregados;
  }
}
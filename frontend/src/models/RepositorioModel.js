export default class RepositorioModel {
  constructor(obj) {
    obj = obj || {};

    this.id = obj.id || null;
    this.url = obj.url || null;
    this.nome = obj.nome || null;
    this.projetos = Array.isArray(obj.projetos)
      ? obj.projetos.map(p => new ProjetoModel(p))
      : [];
    this.agregados = obj.agregados || null;
  }
}

class ProjetoModel {
  constructor(obj) {
    obj = obj || {};

    this.nome = obj.nome || null;
    this.subdiretorio = obj.subdiretorio || null;
    this.perfilVSCode = obj.perfilVSCode || null;
    this.comandos = obj.comandos ? new ComandosModel(obj.comandos) : null;
  }
}

class ComandosModel {
  constructor(obj) {
    obj = obj || {};

    this.instalar = obj.instalar || null;
    this.iniciar = obj.iniciar || null;
    this.buildar = obj.buildar || null;
    this.abrirNoVSCode = obj.abrirNoVSCode ?? false;
  }
}

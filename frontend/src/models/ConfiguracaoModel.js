export default class ConfiguracaoModel{
  constructor(obj) {
    obj = obj || {};


    this.diretorioRaiz = obj.diretorioRaiz;
    this.perfisVSCode = obj.perfisVSCode;
  }
}
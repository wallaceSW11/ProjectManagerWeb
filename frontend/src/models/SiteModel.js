class SiteModel {
  constructor(data = {}) {
    this.nome = data.nome || "";
    this.porta = data.porta || "";
    this.status = data.status || "";
  }

  static fromJson(json) {
    return new SiteModel(json);
  }

  toJson() {
    return {
      nome: this.nome,
      porta: this.porta,
      status: this.status
    };
  }
}

export default SiteModel;
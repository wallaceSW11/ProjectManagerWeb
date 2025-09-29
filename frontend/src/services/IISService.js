import BaseApiService from "./BaseApiService";

class IISService extends BaseApiService {
  async getSites() {
    return await this.get("iis/sites");
  }

  async iniciarSite(nomeSite) {
    return await this.post(`iis/sites/${nomeSite}/iniciar`);
  }

  async pararSite(nomeSite) {
    return await this.post(`iis/sites/${nomeSite}/parar`);
  }

  async reiniciarSite(nomeSite) {
    return await this.post(`iis/sites/${nomeSite}/reiniciar`);
  }
}

export default new IISService();
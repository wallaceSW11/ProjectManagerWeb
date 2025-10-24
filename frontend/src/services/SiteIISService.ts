import BaseApiService from './BaseApiService';
import type { ISiteIIS, ISiteIISDeployResponse, IAtualizarSiteResponse } from '@/models/SiteIISModel';

class SiteIISService extends BaseApiService {
  async getSites(): Promise<ISiteIISDeployResponse[]> {
    return await this.get<ISiteIISDeployResponse[]>('sites-iis');
  }

  async getSiteById(identificador: string): Promise<ISiteIIS> {
    return await this.get<ISiteIIS>(`sites-iis/${identificador}`);
  }

  async adicionarSite(site: ISiteIIS): Promise<ISiteIIS> {
    const dto = (site as any).toDTO ? (site as any).toDTO() : site;
    return await this.post<ISiteIIS>('sites-iis', dto);
  }

  async atualizarSite(site: ISiteIIS): Promise<void> {
    const dto = (site as any).toDTO ? (site as any).toDTO() : site;
    return await this.put(`sites-iis/${site.identificador}`, dto);
  }

  async excluirSite(identificador: string): Promise<void> {
    return await this.delete(`sites-iis/${identificador}`);
  }

  async dispararDeploy(identificador: string): Promise<IAtualizarSiteResponse> {
    return await this.post<IAtualizarSiteResponse>(
      `sites-iis/${identificador}/atualizar`,
      {}
    );
  }
}

export default new SiteIISService();

import type { IPastaMenu } from '@/types';

export default class PastaMenuModel implements IPastaMenu {
  identificador: string;
  origem: string;
  destino: string;

  constructor(obj: Partial<IPastaMenu> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.origem = obj.origem || '';
    this.destino = obj.destino || '';
  }
}

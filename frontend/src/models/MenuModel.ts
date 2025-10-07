import type { IMenu, IArquivo } from '@/types';
import ArquivoModel from './ArquivoModel';

export default class MenuModel implements IMenu {
  identificador: string;
  titulo: string;
  tipo: 'APLICAR_ARQUIVO' | 'COMANDO_AVULSO';
  arquivos: IArquivo[];
  comandos: Array<{ comando: string }>;

  constructor(obj: Partial<IMenu> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.titulo = obj.titulo || '';
    this.tipo = obj.tipo || 'APLICAR_ARQUIVO';
    this.arquivos = obj.arquivos?.map(a => new ArquivoModel(a)) || [];
    this.comandos = obj.comandos || [];
  }
}

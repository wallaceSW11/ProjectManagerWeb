import type { IMenu, IArquivo, IPastaMenu } from '@/types';
import ArquivoModel from './ArquivoModel';
import PastaMenuModel from './PastaMenuModel';

export default class MenuModel implements IMenu {
  identificador: string;
  titulo: string;
  tipo: 'APLICAR_ARQUIVO' | 'APLICAR_PASTA' | 'COMANDO_AVULSO';
  arquivos: IArquivo[];
  pastas: IPastaMenu[];
  comandos: Array<{ comando: string }>;

  constructor(obj: Partial<IMenu> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.titulo = obj.titulo || '';
    this.tipo = obj.tipo || 'APLICAR_ARQUIVO';
    this.arquivos = obj.arquivos?.map(a => new ArquivoModel(a)) || [];
    this.pastas = obj.pastas?.map(p => new PastaMenuModel(p)) || [];
    this.comandos = obj.comandos || [];
  }
}

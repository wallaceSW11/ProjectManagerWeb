import type { IProjeto, IComandos } from '@/types';
import { TIPO_COMANDO } from '@/constants/geral-constants';

export default class ProjetoModel implements IProjeto {
  identificador: string;
  nome: string;
  nomeRepositorio: string;
  subdiretorio: string;
  perfilVSCode: string;
  comandos: string[];
  arquivoCoverage: string;
  expandido: boolean;

  constructor(obj: Partial<IProjeto> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.nome = obj.nome || '';
    this.nomeRepositorio = obj.nomeRepositorio || '';
    this.subdiretorio = obj.subdiretorio || '';
    this.perfilVSCode = obj.perfilVSCode || '';
    this.comandos = obj.comandos || [];
    this.arquivoCoverage = obj.arquivoCoverage || '';
    this.expandido = obj.expandido || false;
  }

  /**
   * Converte array de comandos para objeto IComandos para compatibilidade com formulários
   */
  get comandosObj(): IComandos {
    return {
      instalar: this.comandos.includes(TIPO_COMANDO.INSTALAR.valor) ? 'npm install' : null,
      iniciar: this.comandos.includes(TIPO_COMANDO.INICIAR.valor) ? 'npm start' : null,
      buildar: this.comandos.includes(TIPO_COMANDO.BUILDAR.valor) ? 'npm run build' : null,
      abrirNoVSCode: this.comandos.includes(TIPO_COMANDO.ABRIR_NO_VSCODE.valor)
    };
  }

  /**
   * Converte objeto IComandos para array de strings
   */
  set comandosObj(comandos: IComandos) {
    this.comandos = [];
    
    if (comandos.instalar) {
      this.comandos.push(TIPO_COMANDO.INSTALAR.valor);
    }
    if (comandos.iniciar) {
      this.comandos.push(TIPO_COMANDO.INICIAR.valor);
    }
    if (comandos.buildar) {
      this.comandos.push(TIPO_COMANDO.BUILDAR.valor);
    }
    if (comandos.abrirNoVSCode) {
      this.comandos.push(TIPO_COMANDO.ABRIR_NO_VSCODE.valor);
    }
  }

  /**
   * Converte os comandos do projeto em um array de objetos para exibição na interface
   */
  getComandosDisponiveis(): Array<{ titulo: string; valor: string; ativo: boolean }> {
    const comandosDisponiveis = [];

    for (const comando of this.comandos) {
      switch (comando) {
        case TIPO_COMANDO.INSTALAR.valor:
          comandosDisponiveis.push({
            titulo: TIPO_COMANDO.INSTALAR.titulo,
            valor: TIPO_COMANDO.INSTALAR.valor,
            ativo: true
          });
          break;
        case TIPO_COMANDO.INICIAR.valor:
          comandosDisponiveis.push({
            titulo: TIPO_COMANDO.INICIAR.titulo,
            valor: TIPO_COMANDO.INICIAR.valor,
            ativo: true
          });
          break;
        case TIPO_COMANDO.BUILDAR.valor:
          comandosDisponiveis.push({
            titulo: TIPO_COMANDO.BUILDAR.titulo,
            valor: TIPO_COMANDO.BUILDAR.valor,
            ativo: true
          });
          break;
        case TIPO_COMANDO.ABRIR_NO_VSCODE.valor:
          comandosDisponiveis.push({
            titulo: TIPO_COMANDO.ABRIR_NO_VSCODE.titulo,
            valor: TIPO_COMANDO.ABRIR_NO_VSCODE.valor,
            ativo: true
          });
          break;
      }
    }

    return comandosDisponiveis;
  }
}

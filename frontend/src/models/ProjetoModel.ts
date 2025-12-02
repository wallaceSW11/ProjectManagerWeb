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
  identificadorRepositorioAgregado?: string;
  comandosObj: IComandos;
  nomeIDE?: string | null;

  constructor(obj: Partial<IProjeto> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.nome = obj.nome || '';
    this.nomeRepositorio = obj.nomeRepositorio || '';
    this.subdiretorio = obj.subdiretorio || '';
    this.perfilVSCode = obj.perfilVSCode || '';
    this.comandos = obj.comandos || [];
    this.arquivoCoverage = obj.arquivoCoverage || '';
    this.expandido = obj.expandido || false;
    this.identificadorRepositorioAgregado = obj.identificadorRepositorioAgregado;
    this.nomeIDE = obj.nomeIDE;
    
    // Handle comandosObj from backend or frontend
    if (obj.comandosObj) {
      this.comandosObj = obj.comandosObj;
    } else if ((obj as any).comandos && typeof (obj as any).comandos === 'object' && !(obj as any).comandos.length) {
      // Repository backend sends comandos as ComandoDTO object
      this.comandosObj = (obj as any).comandos;
      // Auto-sync comandos array for display
      this.syncComandosFromObj();
    } else {
      // Default or pasta response (comandos already as array)
      this.comandosObj = {
        instalar: null,
        iniciar: null,
        buildar: null,
        ideIdentificador: null
      };
    }
  }

  /**
   * Sincroniza comandosObj com array de comandos (para execução)
   */
  syncComandosFromObj(): void {
    this.comandos = [];
    
    if (this.comandosObj.instalar) {
      this.comandos.push(TIPO_COMANDO.INSTALAR.valor);
    }
    if (this.comandosObj.iniciar) {
      this.comandos.push(TIPO_COMANDO.INICIAR.valor);
    }
    if (this.comandosObj.buildar) {
      this.comandos.push(TIPO_COMANDO.BUILDAR.valor);
    }
    if (this.comandosObj.ideIdentificador) {
      this.comandos.push(TIPO_COMANDO.ABRIR_NA_IDE.valor);
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
        case TIPO_COMANDO.ABRIR_NA_IDE.valor:
          // Usa o nome da IDE que veio do backend
          const tituloIDE = this.nomeIDE 
            ? `Abrir no ${this.nomeIDE}` 
            : TIPO_COMANDO.ABRIR_NA_IDE.titulo;
          comandosDisponiveis.push({
            titulo: tituloIDE,
            valor: TIPO_COMANDO.ABRIR_NA_IDE.valor,
            ativo: true
          });
          break;
        // Ignorar comandos desconhecidos (ex: ABRIR_NO_VSCODE antigo)
        default:
          console.warn(`Comando desconhecido ignorado: ${comando}`);
          break;
      }
    }

    return comandosDisponiveis;
  }

  /**
   * Converte para o formato DTO esperado pelo backend
   */
  toDTO() {
    return {
      identificador: this.identificador,
      nome: this.nome,
      subdiretorio: this.subdiretorio || null,
      perfilVSCode: this.perfilVSCode || null,
      comandos: {
        instalar: this.comandosObj.instalar,
        iniciar: this.comandosObj.iniciar,
        buildar: this.comandosObj.buildar,
        ideIdentificador: this.comandosObj.ideIdentificador
      },
      arquivoCoverage: this.arquivoCoverage || null,
      expandido: this.expandido
    };
  }
}

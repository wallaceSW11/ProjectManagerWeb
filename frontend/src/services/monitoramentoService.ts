import MonitoramentoModel from '@/models/MonitoramentoModel';
import type { IMonitoramentoSnapshot } from '@/types';

class MonitoramentoService {
  private socket: WebSocket | null = null;
  private url: string;
  private timerReconexao: ReturnType<typeof setTimeout> | null = null;
  private tentativas: number = 0;
  private desconexaoIntencional: boolean = false;
  private onMessageCallback: ((data: IMonitoramentoSnapshot) => void) | null =
    null;
  private onStatusCallback: ((conectado: boolean) => void) | null = null;

  constructor() {
    const isDev = import.meta.env.DEV;
    const host = isDev ? 'localhost:2024' : location.host;
    const protocol = location.protocol === 'https:' ? 'wss' : 'ws';
    this.url = `${protocol}://${host}/api/monitoramento/ws`;
  }

  conectar(
    onMessage: (data: IMonitoramentoSnapshot) => void,
    onStatusChange: (conectado: boolean) => void
  ): void {
    this.desconexaoIntencional = false;
    this.onMessageCallback = onMessage;
    this.onStatusCallback = onStatusChange;

    const socket = new WebSocket(this.url);
    this.socket = socket;

    socket.onopen = () => {
      this.tentativas = 0;
      this.onStatusCallback?.(true);
    };

    socket.onmessage = (event: MessageEvent) => {
      const snapshot = new MonitoramentoModel(JSON.parse(event.data));
      this.onMessageCallback?.(snapshot);
    };

    socket.onclose = () => {
      this.tratarFechamento();
    };
  }

  desconectar(): void {
    this.desconexaoIntencional = true;
    if (this.timerReconexao) {
      clearTimeout(this.timerReconexao);
      this.timerReconexao = null;
    }
    if (this.socket) {
      this.socket.close(1000, 'desconexão manual');
      this.socket = null;
    }
  }

  private tratarFechamento(): void {
    this.onStatusCallback?.(false);
    if (this.desconexaoIntencional) return;
    this.agendarReconexao();
  }

  private agendarReconexao(): void {
    if (this.timerReconexao) clearTimeout(this.timerReconexao);

    const delay = Math.min(2000 * Math.pow(2, this.tentativas), 30000);
    this.tentativas++;
    this.timerReconexao = setTimeout(() => {
      this.timerReconexao = null;
      if (!this.onMessageCallback || !this.onStatusCallback) return;
      this.conectar(this.onMessageCallback, this.onStatusCallback);
    }, delay);
  }
}

export default MonitoramentoService;

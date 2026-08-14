import type { IMonitoramentoSnapshot } from '@/types';

export default class MonitoramentoModel implements IMonitoramentoSnapshot {
  timestamp: string;
  plataforma: string;
  clientesConectados: number;
  contadorSnapshots: number;
  sistemaOperacional: string;
  cpuPercentual: number | null;
  ramTotalBytes: number | null;
  ramDisponivelBytes: number | null;
  ramUsadaBytes: number | null;
  discoPercentual: number | null;
  discoTotalBytes: number | null;
  discoDisponivelBytes: number | null;
  discoUsadaBytes: number | null;

  constructor(obj: Partial<IMonitoramentoSnapshot> = {}) {
    this.timestamp = obj.timestamp || new Date().toISOString();
    this.plataforma = obj.plataforma || '';
    this.clientesConectados = obj.clientesConectados || 0;
    this.contadorSnapshots = obj.contadorSnapshots || 0;
    this.sistemaOperacional = obj.sistemaOperacional || '';
    this.cpuPercentual = obj.cpuPercentual ?? null;
    this.ramTotalBytes = obj.ramTotalBytes ?? null;
    this.ramDisponivelBytes = obj.ramDisponivelBytes ?? null;
    this.ramUsadaBytes = obj.ramUsadaBytes ?? null;
    this.discoPercentual = obj.discoPercentual ?? null;
    this.discoTotalBytes = obj.discoTotalBytes ?? null;
    this.discoDisponivelBytes = obj.discoDisponivelBytes ?? null;
    this.discoUsadaBytes = obj.discoUsadaBytes ?? null;
  }
}

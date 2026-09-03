import type { IMonitoramentoSnapshot } from '@/types';

export default class MonitoramentoModel implements IMonitoramentoSnapshot {
  timestamp: string;
  plataforma: string;
  clientesConectados: number;
  contadorSnapshots: number;
  sistemaOperacional: string;
  cpuPercentual: number | null;
  cpuNome: string | null;
  cpuFrequenciaMhz: number | null;
  cpuTemperaturaCelsius: number | null;
  ramTotalBytes: number | null;
  ramDisponivelBytes: number | null;
  ramUsadaBytes: number | null;
  ramVelocidadeMhz: number | null;
  discoPercentual: number | null;
  discoTotalBytes: number | null;
  discoDisponivelBytes: number | null;
  discoUsadaBytes: number | null;
  discoTemperaturaCelsius: number | null;
  coolerRpm: number | null;
  swapTotalBytes: number | null;
  swapUsadaBytes: number | null;
  redeDownloadBytesPorSegundo: number | null;
  redeUploadBytesPorSegundo: number | null;

  constructor(obj: Partial<IMonitoramentoSnapshot> = {}) {
    this.timestamp = obj.timestamp || new Date().toISOString();
    this.plataforma = obj.plataforma || '';
    this.clientesConectados = obj.clientesConectados || 0;
    this.contadorSnapshots = obj.contadorSnapshots || 0;
    this.sistemaOperacional = obj.sistemaOperacional || '';
    this.cpuPercentual = obj.cpuPercentual ?? null;
    this.cpuNome = obj.cpuNome ?? null;
    this.cpuFrequenciaMhz = obj.cpuFrequenciaMhz ?? null;
    this.cpuTemperaturaCelsius = obj.cpuTemperaturaCelsius ?? null;
    this.ramTotalBytes = obj.ramTotalBytes ?? null;
    this.ramDisponivelBytes = obj.ramDisponivelBytes ?? null;
    this.ramUsadaBytes = obj.ramUsadaBytes ?? null;
    this.ramVelocidadeMhz = obj.ramVelocidadeMhz ?? null;
    this.discoPercentual = obj.discoPercentual ?? null;
    this.discoTotalBytes = obj.discoTotalBytes ?? null;
    this.discoDisponivelBytes = obj.discoDisponivelBytes ?? null;
    this.discoUsadaBytes = obj.discoUsadaBytes ?? null;
    this.discoTemperaturaCelsius = obj.discoTemperaturaCelsius ?? null;
    this.coolerRpm = obj.coolerRpm ?? null;
    this.swapTotalBytes = obj.swapTotalBytes ?? null;
    this.swapUsadaBytes = obj.swapUsadaBytes ?? null;
    this.redeDownloadBytesPorSegundo = obj.redeDownloadBytesPorSegundo ?? null;
    this.redeUploadBytesPorSegundo = obj.redeUploadBytesPorSegundo ?? null;
  }
}

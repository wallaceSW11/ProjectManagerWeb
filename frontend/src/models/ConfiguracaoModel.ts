import type { IConfiguracao, IUsoRecursos } from '@/types';

export default class ConfiguracaoModel implements IConfiguracao {
  diretorioRaiz: string;
  perfisVSCode: Array<{ nome: string }>;
  clis: Array<{ nome: string; comando: string }>;
  terminalLinux: string;
  pastasCentralizadoras: Array<{ nome: string }>;
  usoRecursos: IUsoRecursos;

  constructor(obj: Partial<IConfiguracao> = {}) {
    this.diretorioRaiz = obj.diretorioRaiz || '';
    this.perfisVSCode = obj.perfisVSCode || [];
    this.clis = obj.clis || [];
    this.terminalLinux = obj.terminalLinux || 'ptyxis';
    this.pastasCentralizadoras = obj.pastasCentralizadoras || [];
    this.usoRecursos = {
      ativo: !!obj.usoRecursos?.ativo,
      limiarCpuPercentual: obj.usoRecursos?.limiarCpuPercentual ?? 80,
      intervaloRegistroSegundos:
        obj.usoRecursos?.intervaloRegistroSegundos ?? 5,
      retencaoDias: obj.usoRecursos?.retencaoDias ?? 30
    };
  }
}

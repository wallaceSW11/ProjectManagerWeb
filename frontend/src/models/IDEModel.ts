import type { IIDE } from '@/types';

export default class IDEModel implements IIDE {
  identificador: string;
  nome: string;
  comandoParaExecutar: string;
  aceitaPerfilPersonalizado: boolean;

  constructor(obj: Partial<IIDE> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID();
    this.nome = obj.nome || '';
    this.comandoParaExecutar = obj.comandoParaExecutar || '';
    this.aceitaPerfilPersonalizado = obj.aceitaPerfilPersonalizado ?? false;
  }

  /**
   * Converte para o formato DTO esperado pelo backend
   */
  toDTO() {
    return {
      identificador: this.identificador,
      nome: this.nome,
      comandoParaExecutar: this.comandoParaExecutar,
      aceitaPerfilPersonalizado: this.aceitaPerfilPersonalizado
    };
  }
}

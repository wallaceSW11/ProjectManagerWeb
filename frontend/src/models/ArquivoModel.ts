import type { IArquivo } from '@/types'

export default class ArquivoModel implements IArquivo {
  identificador: string
  arquivo: string
  destino: string
  ignorarGit: boolean

  constructor(obj: Partial<IArquivo> = {}) {
    this.identificador = obj.identificador || crypto.randomUUID()
    this.arquivo = obj.arquivo || ''
    this.destino = obj.destino || ''
    this.ignorarGit = obj.ignorarGit || false
  }
}
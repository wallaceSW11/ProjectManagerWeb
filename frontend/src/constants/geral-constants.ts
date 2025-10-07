interface ModoOperacaoItem {
  titulo: string
  valor: string
}

interface ModoOperacaoConstants {
  INICIAL: ModoOperacaoItem
  NOVO: ModoOperacaoItem
  EDICAO: ModoOperacaoItem
}

export const MODO_OPERACAO: ModoOperacaoConstants = {
  INICIAL: {
    titulo: 'Adicionar',
    valor: 'ADICIONAR',
  },
  NOVO: {
    titulo: 'Novo',
    valor: 'NOVO',
  },
  EDICAO: {
    titulo: 'Editar',
    valor: 'EDITAR',
  },
}

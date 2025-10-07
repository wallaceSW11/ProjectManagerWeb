interface ModoOperacaoItem {
  titulo: string;
  valor: string;
}

interface ModoOperacaoConstants {
  INICIAL: ModoOperacaoItem;
  NOVO: ModoOperacaoItem;
  EDICAO: ModoOperacaoItem;
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
};

interface TipoComandoItem {
  titulo: string;
  valor: string;
}

interface TipoComandoConstants {
  INSTALAR: TipoComandoItem;
  INICIAR: TipoComandoItem;
  BUILDAR: TipoComandoItem;
  ABRIR_NO_VSCODE: TipoComandoItem;
}

export const TIPO_COMANDO: TipoComandoConstants = {
  INSTALAR: {
    titulo: 'Instalar',
    valor: 'INSTALAR',
  },
  INICIAR: {
    titulo: 'Iniciar',
    valor: 'INICIAR',
  },
  BUILDAR: {
    titulo: 'Buildar',
    valor: 'BUILDAR',
  },
  ABRIR_NO_VSCODE: {
    titulo: 'Abrir no VS Code',
    valor: 'ABRIR_NO_VSCODE',
  },
};

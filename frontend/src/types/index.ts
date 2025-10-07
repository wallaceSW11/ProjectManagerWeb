export interface IArquivo {
  identificador: string
  arquivo: string
  destino: string
  ignorarGit: boolean
}

export interface IMenu {
  identificador: string
  titulo: string
  tipo: 'APLICAR_ARQUIVO' | 'COMANDO_AVULSO'
  arquivos: IArquivo[]
  comandos: Array<{ comando: string }>
}

export interface IComandos {
  instalar: string | null
  iniciar: string | null
  buildar: string | null
  abrirNoVSCode: boolean
}

export interface IProjeto {
  identificador: string
  nome: string
  nomeRepositorio: string
  subdiretorio: string
  perfilVSCode: string
  comandos: IComandos
  arquivoCoverage: string
  comandosSelecionados?: string[]
  identificadorRepositorioAgregado?: string
}

export interface IRepositorio {
  identificador: string
  url: string | null
  titulo: string
  nome: string
  projetos: IProjeto[]
  agregados: string[]
  menus: IMenu[]
}

export interface IPasta {
  identificador: string
  diretorio: string
  codigo: string
  descricao: string
  tipo: string
  branch: string
  git: string
  repositorioId?: string
  projetos: IProjeto[]
  menus: IMenu[]
  index: number
}

export interface IClone {
  diretorioRaiz: string
  codigo: string
  descricao: string
  tipo: 'nenhum' | 'feature' | 'bug' | 'hotfix'
  branch: string
  repositorio: IRepositorio
  criarBranchRemoto: boolean
  baixarAgregados: boolean
}

export interface IPerfilVSCode {
  nome: string
}

export interface IConfiguracao {
  diretorioRaiz: string
  perfisVSCode: Array<{ nome: string }>
}

export interface ISite {
  nome: string
  porta: string
  status: string
}

export interface NotificacaoTipo {
  tipo: 'sucesso' | 'erro' | 'aviso'
  titulo: string
  mensagem?: string
}

export interface CarregandoInfo {
  exibir: boolean
  texto: string
}

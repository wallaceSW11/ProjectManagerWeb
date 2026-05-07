import BaseApiService from './BaseApiService';

interface ComandoExecucao {
  diretorio: string;
  repositorioId?: string;
  projetos: Array<{
    identificador: string;
    nome: string;
    comandos: string[];
    identificadorRepositorioAgregado?: string;
    nomeRepositorio: string;
  }>;
}

interface ComandoMenu {
  diretorio: string;
  repositorioId?: string;
  comandoId: string;
}

interface ComandoAvulso {
  comando: string;
  perfilTerminal?: string | null;
}

interface AbrirPastaIDE {
  diretorio: string;
  ideIdentificador: string;
  perfilVSCode?: string | null;
}

class ComandosService extends BaseApiService {
  async executarComando(comandos: ComandoExecucao): Promise<void> {
    return await this.post('comandos', comandos);
  }

  async executarComandoMenu(comando: ComandoMenu): Promise<void> {
    return await this.post('comandos/menu', comando);
  }

  async executarComandoAvulso(comando: ComandoAvulso): Promise<void> {
    return await this.post('comandos/avulso', comando);
  }

  async abrirPastaIDE(request: AbrirPastaIDE): Promise<void> {
    return await this.post('comandos/abrir-pasta-ide', request);
  }
}

export default new ComandosService();

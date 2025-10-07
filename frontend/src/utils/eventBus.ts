import mitt, { Emitter } from 'mitt';
import type { NotificacaoTipo, CarregandoInfo } from '@/types';

type Events = {
  carregando: CarregandoInfo;
  notificar: NotificacaoTipo;
  atualizarListaPastas: void;
};

const emitter: Emitter<Events> = mitt<Events>();

function carregando(exibir: boolean, texto = 'Carregando...'): void {
  emitter.emit('carregando', { exibir, texto });
}

async function carregandoAsync<T>(
  promiseOrFn: Promise<T> | (() => Promise<T>),
  texto = 'Carregando...'
): Promise<T> {
  try {
    carregando(true, texto);
    let result: T;
    if (typeof promiseOrFn === 'function') {
      result = await promiseOrFn();
    } else {
      result = await promiseOrFn;
    }
    return result;
  } finally {
    carregando(false);
  }
}

function notificar(
  tipo: 'sucesso' | 'erro' | 'aviso',
  titulo: string,
  mensagem = ''
): void {
  emitter.emit('notificar', { tipo, titulo, mensagem });
}

function atualizarListaPastas(): void {
  emitter.emit('atualizarListaPastas');
}

export default emitter;
export { carregando, carregandoAsync, notificar, atualizarListaPastas };

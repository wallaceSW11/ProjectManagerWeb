// src/eventBus.js
import mitt from 'mitt'

const emitter = mitt()

// helper simples para emitir o overlay
function carregando(exibir, texto = 'Carregando...') {
  emitter.emit('carregando', { exibir, texto })
}

/**
 * Helper para executar uma promise com overlay automaticamente.
 * @param {Promise|Function} promiseOrFn - Promise ou função async
 * @param {string} texto - mensagem do overlay
 */
async function carregandoAsync(promiseOrFn, texto = 'Carregando...') {
  try {
    // abre overlay
    carregando(true, texto)

    let result
    if (typeof promiseOrFn === 'function') {
      result = await promiseOrFn()
    } else {
      result = await promiseOrFn
    }

    return result
  } finally {
    // fecha overlay sempre
    carregando(false)
  }
}

export default emitter
export { carregando, carregandoAsync }

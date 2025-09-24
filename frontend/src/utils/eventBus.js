import mitt from "mitt";

const emitter = mitt();

// Overlay helpers (se quiser continuar usando)
function carregando(exibir, texto = "Carregando...") {
  emitter.emit("carregando", { exibir, texto });
}

async function carregandoAsync(promiseOrFn, texto = "Carregando...") {
  try {
    carregando(true, texto);
    let result;
    if (typeof promiseOrFn === "function") {
      result = await promiseOrFn();
    } else {
      result = await promiseOrFn;
    }
    return result;
  } finally {
    carregando(false);
  }
}

// Notificações / Toast
function notificar(tipo, titulo, mensagem = "") {
  emitter.emit("notificar", { tipo, titulo, mensagem });
}

function atualizarListaPastas() {
  emitter.emit("atualizarListaPastas");
}

export default emitter;
export { carregando, carregandoAsync, notificar, atualizarListaPastas };

import { validarArquivo } from "../lib/code-style-regras.js";

const EXTENSOES_CODIGO = ["cs", "vue", "ts"];

export default async ({ client }) => {
  const arquivosPendentes = new Set();

  const registrar = (caminho) => {
    if (!caminho) return;
    const ext = caminho.split(".").pop()?.toLowerCase();
    if (EXTENSOES_CODIGO.includes(ext)) arquivosPendentes.add(caminho);
  };

  const extrairCaminho = (args) => args?.filePath || args?.path || null;

  const validarPendentes = async () => {
    if (arquivosPendentes.size === 0) return;

    const caminhos = [...arquivosPendentes];
    arquivosPendentes.clear();

    let totalViolacoes = 0;
    let arquivosComViolacao = 0;

    for (const caminho of caminhos) {
      let conteudo;
      try {
        conteudo = await Bun.file(caminho).text();
      } catch {
        continue;
      }

      const violacoes = validarArquivo(caminho, conteudo);
      if (violacoes.length === 0) continue;

      totalViolacoes += violacoes.length;
      arquivosComViolacao++;
    }

    const mensagem = totalViolacoes > 0
      ? `${totalViolacoes} violação(ões) de code style em ${arquivosComViolacao} arquivo(s). Rode /validar-code-style.`
      : `Code style OK em ${caminhos.length} arquivo(s)`;

    try {
      await client.tui.showToast({
        body: {
          title: "CODE-STYLE",
          message: mensagem,
          variant: totalViolacoes > 0 ? "error" : "success"
        }
      });
    } catch {
    }
  };

  return {
    "tool.execute.after": async (input) => {
      if (input.tool !== "write" && input.tool !== "apply_patch" && input.tool !== "edit") return;
      registrar(extrairCaminho(input.args));
    },

    event: async ({ event }) => {
      if (event.type === "file.edited") {
        registrar(event.properties?.path);
        return;
      }

      if (event.type === "session.idle")
        await validarPendentes();
    }
  };
};

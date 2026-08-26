import { validarArquivo } from "../lib/code-style-regras.js";

export default async () => ({
  "tool.execute.after": async (input, _output) => {
    if (input.tool !== "write" && input.tool !== "apply_patch" && input.tool !== "edit") return;

    const caminho = input.args?.path || input.args?.filePath;
    if (!caminho) return;

    let conteudo;
    try {
      conteudo = await Bun.file(caminho).text();
    } catch {
      return;
    }

    const violacoes = validarArquivo(caminho, conteudo);
    if (violacoes.length > 0) {
      const feedback = [
        "CODE-STYLE: Violações em '" + caminho + "'. Corrija agora:",
        ...violacoes.map(v => "  - " + v),
        "",
        "Releia o arquivo, corrija as violações e salve novamente."
      ].join("\n");
      console.log(feedback);
    }
  }
});

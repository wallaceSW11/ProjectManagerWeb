import { readFile } from "node:fs/promises";
import { Plugin } from "@opencode/plugin";
import { validarArquivo } from "./code-style.ts";
import { ehArquivoInfra, validarInfra } from "./infra.ts";

const FERRAMENTAS_DE_EDICAO = new Set(["write", "edit", "patch", "apply_patch"]);

type EntradaFerramenta = {
  path?: string;
  filePath?: string;
  file_path?: string;
};

const caminhoDoInput = (input: unknown): string | null => {
  const entrada = input as EntradaFerramenta | null;

  return entrada?.path ?? entrada?.filePath ?? entrada?.file_path ?? null;
};

const validar = (caminho: string, conteudo: string): string[] => {
  const violacoes = [...validarArquivo(caminho, conteudo)];

  if (ehArquivoInfra(caminho)) violacoes.push(...validarInfra(caminho));

  return violacoes;
};

export default Plugin.define({
  id: "pmw.validacao",

  async setup(ctx) {
    const pendencias = new Map<string, string[]>();

    await ctx.tool.hook("execute.after", async (event) => {
      if (!FERRAMENTAS_DE_EDICAO.has(event.tool)) return;
      if (event.status !== "completed") return;

      const caminho = caminhoDoInput(event.input);
      if (!caminho) return;

      const conteudo = await readFile(caminho, "utf8").catch(() => null);
      if (conteudo === null) return;

      const violacoes = validar(caminho, conteudo);

      if (violacoes.length === 0) {
        pendencias.delete(caminho);
        return;
      }

      pendencias.set(caminho, violacoes);
    });

    await ctx.session.hook("context", (event) => {
      if (pendencias.size === 0) return;

      const linhas = [...pendencias].flatMap(([caminho, violacoes]) => [
        `- ${caminho}`,
        ...violacoes.map((violacao) => `  - ${violacao}`),
      ]);

      event.system.push({
        type: "text",
        text: [
          "VALIDAÇÃO DO PROJETO: violações pendentes nos arquivos que você editou.",
          "Corrija antes de responder (rode a skill validar-code-style se precisar do checklist).",
          ...linhas,
        ].join("\n"),
      });
    });
  },
});

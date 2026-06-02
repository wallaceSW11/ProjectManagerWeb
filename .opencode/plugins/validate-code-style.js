export const PluginValidarCodeStyle = async () => ({
  "tool.execute.after": async (input, _output) => {
    if (input.tool !== "write" && input.tool !== "apply_patch") return;

    const caminho = input.args?.path || input.args?.filePath;
    if (!caminho) return;

    const ext = caminho.split(".").pop()?.toLowerCase();
    if (ext !== "cs" && ext !== "vue" && ext !== "ts") return;

    let conteudo;
    try {
      conteudo = await Bun.file(caminho).text();
    } catch {
      return;
    }

    const violacoes = [];

    if (ext === "cs") {
      if (/AutoMapper|CreateMap|IMapper/.test(conteudo))
        violacoes.push("PROIBIDO: AutoMapper detectado. Use mapeamento manual.");
      if (/\.(Result|Wait\(\))/.test(conteudo))
        violacoes.push("PROIBIDO: .Result/.Wait() detectado. Use async/await.");
      if (/Console\.Write(Line)?/.test(conteudo) && !caminho.endsWith("Program.cs"))
        violacoes.push("AVISO: Console.WriteLine detectado. Remova debug.");
      if (/catch\s*\([^)]*\)\s*\{\s*\}/.test(conteudo))
        violacoes.push("PROIBIDO: Exceção engolida (catch vazio).");
      if (/Controller\.cs$/.test(caminho)) {
        if (/File\.(ReadAll|WriteAll|Exists)|JsonSerializer\.(Serialize|Deserialize)/.test(conteudo))
          violacoes.push("PROIBIDO: Controller acessando JSON direto. Delegue ao Service.");
        if (/ActionResult</.test(conteudo))
          violacoes.push("AVISO: Use IActionResult, não ActionResult<T>.");
      }
    }

    if (ext === "vue" || ext === "ts") {
      if (/\.then\s*\(/.test(conteudo))
        violacoes.push("PROIBIDO: .then() detectado. Use async/await.");
      if (/\bvar\s+\w+\s*[=;,:]/.test(conteudo))
        violacoes.push("PROIBIDO: 'var' detectado. Use const/let.");
      if (/console\.(log|warn|info|debug)/.test(conteudo))
        violacoes.push("PROIBIDO: console.log detectado. Remova.");
      if (/!important/.test(conteudo))
        violacoes.push("PROIBIDO: !important no CSS. Use classes Vuetify.");
      if (/\bis[A-Z]\w*\s*[=:]|\bhas[A-Z]\w*\s*[=:]/.test(conteudo))
        violacoes.push("AVISO: Prefixo is/has em booleano. Use sem prefixo.");
    }

    if (ext === "vue") {
      const match = conteudo.match(/<template>([\s\S]*?)<\/template>/);
      if (match) {
        const template = match[1];
        if (/v-if="[^"]*(\&\&|\|\||\.length|\.filter|\.map|\.find|\?.*:)/.test(template) ||
            /v-show="[^"]*(\&\&|\|\||\.length|\.filter|\.map|\.find|\?.*:)/.test(template))
          violacoes.push("PROIBIDO: Lógica no template. Extraia pra computed.");
        if (/:class="[^"]*\?/.test(template))
          violacoes.push("PROIBIDO: Ternário em :class. Extraia pra computed.");
      }
    }

    if (ext === "ts" && /Store|store/.test(caminho)) {
      if (/(loading|carregando)\s*=\s*(false|true)/.test(conteudo) && !/finally/.test(conteudo))
        violacoes.push("AVISO: Loading sem finally. Reset no finally.");
    }

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

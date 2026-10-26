export function contarStatementsNoBloco(bloco: string): number {
  const corpo = bloco.replace(/^\{/, "").replace(/\}$/, "").trim();

  if (!corpo) return 0;

  return corpo.split(";").filter((s) => s.trim()).length;
}

function detectarComentarios(conteudo: string): string[] {
  const violacoes: string[] = [];

  conteudo.split("\n").forEach((linha, indice) => {
    const limpa = linha.trim();

    if (!limpa.startsWith("//")) return;
    if (/^\/\/\s*(https?:\/\/|Copyright|License|MIT|Apache)/.test(limpa)) return;
    if (/^\/\/\s*(errado|certo)/.test(limpa)) return;
    if (/^\/\/\s*$/.test(limpa)) return;

    violacoes.push(`PROIBIDO: comentário no código (linha ${indice + 1}). Zero comentários.`);
  });

  return violacoes;
}

function detectarIfComChaves(conteudo: string): string[] {
  const violacoes: string[] = [];
  const regex = /if\s*\([^)]*\)\s*\{[\s\S]*?\}/g;
  let achado = regex.exec(conteudo);

  while (achado !== null) {
    const total = contarStatementsNoBloco(achado[0]);

    if (total <= 1) {
      const linha = conteudo.substring(0, achado.index).split("\n").length;
      violacoes.push(`PROIBIDO: if com chaves desnecessárias (linha ${linha}). Use if sem chaves.`);
    }

    achado = regex.exec(conteudo);
  }

  return violacoes;
}

function detectarIfElse(conteudo: string): string[] {
  const regex = /if\s*\([^)]*\)\s*\{[^}]*\}\s*else\s*(if\s*\([^)]*\)\s*\{[^}]*\}|)/;

  return regex.test(conteudo)
    ? ["AVISO: if/else detectado. Use ternário pra 2 caminhos ou objetos mapeados."]
    : [];
}

function detectarViolacoesCs(caminho: string, conteudo: string): string[] {
  const violacoes: string[] = [];

  if (/AutoMapper|CreateMap|IMapper/.test(conteudo))
    violacoes.push("PROIBIDO: AutoMapper detectado. Use mapeamento manual.");
  if (/\.(Result|Wait\(\))/.test(conteudo))
    violacoes.push("PROIBIDO: .Result/.Wait() detectado. Use async/await.");
  if (/Console\.Write(Line)?/.test(conteudo) && !caminho.endsWith("Program.cs"))
    violacoes.push("AVISO: Console.WriteLine detectado. Remova debug.");
  if (/catch\s*\([^)]*\)\s*\{\s*\}/.test(conteudo))
    violacoes.push("PROIBIDO: exceção engolida (catch vazio).");
  if (/Controller\.cs$/.test(caminho) && /File\.(ReadAll|WriteAll|Exists)|JsonSerializer\.(Serialize|Deserialize)/.test(conteudo))
    violacoes.push("PROIBIDO: controller acessando JSON direto. Delegue ao JsonService.");
  if (/Controller\.cs$/.test(caminho) && /ActionResult</.test(conteudo))
    violacoes.push("PROIBIDO: use IActionResult, não ActionResult<T>.");

  return violacoes;
}

function detectarViolacoesTs(conteudo: string): string[] {
  const violacoes: string[] = [];

  if (/\.then\s*\(/.test(conteudo))
    violacoes.push("PROIBIDO: .then() detectado. Use async/await.");
  if (/\bvar\s+\w+\s*[=;,:]/.test(conteudo))
    violacoes.push("PROIBIDO: 'var' detectado. Use const/let.");
  if (/console\.(log|warn|info|debug)/.test(conteudo))
    violacoes.push("PROIBIDO: console.log detectado. Remova.");
  if (/!important/.test(conteudo))
    violacoes.push("PROIBIDO: !important no CSS. Use classes Vuetify.");
  if (/\bis[A-Z]\w*\s*[=:]|\bhas[A-Z]\w*\s*[=:]/.test(conteudo))
    violacoes.push("AVISO: prefixo is/has em booleano. Use sem prefixo.");

  return violacoes;
}

function detectarViolacoesVue(conteudo: string): string[] {
  const template = conteudo.match(/<template>([\s\S]*?)<\/template>/)?.[1];

  if (!template) return [];

  const violacoes: string[] = [];
  const logica = /v-(if|show)="[^"]*(&&|\|\||\.length|\.filter|\.map|\.find|\?.*:)/;

  if (logica.test(template))
    violacoes.push("PROIBIDO: lógica no template. Extraia pra computed.");
  if (/:class="[^"]*\?/.test(template))
    violacoes.push("PROIBIDO: ternário em :class. Extraia pra computed.");

  return violacoes;
}

function detectarViolacoesStore(caminho: string, conteudo: string): string[] {
  if (!/Store|store/.test(caminho)) return [];
  if (!/(loading|carregando)\s*=\s*(false|true)/.test(conteudo)) return [];
  if (/finally/.test(conteudo)) return [];

  return ["AVISO: loading sem finally. Reset no finally."];
}

export function validarArquivo(caminho: string, conteudo: string): string[] {
  const extensao = caminho.split(".").pop()?.toLowerCase();

  if (extensao !== "cs" && extensao !== "vue" && extensao !== "ts") return [];

  const violacoes = [
    ...detectarComentarios(conteudo),
    ...detectarIfComChaves(conteudo),
    ...detectarIfElse(conteudo),
    ...detectarViolacoesStore(caminho, conteudo),
  ];

  if (extensao === "cs") violacoes.push(...detectarViolacoesCs(caminho, conteudo));
  if (extensao === "vue" || extensao === "ts") violacoes.push(...detectarViolacoesTs(conteudo));
  if (extensao === "vue") violacoes.push(...detectarViolacoesVue(conteudo));

  return violacoes;
}

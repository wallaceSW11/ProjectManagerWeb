const ARQUIVOS_INFRA = [
  "bootstrap.sh",
  "bootstrap.ps1",
  "Atualizar_PMW.ps1",
  "infra/pmw.sh",
  "infra/pmw.ps1",
  "infra/pmw.service",
];

function ehArquivoInfra(caminho) {
  return ARQUIVOS_INFRA.some(a => caminho.endsWith(a) || caminho.replace(/\\/g, "/").endsWith(a));
}

async function validarBash(caminho) {
  const proc = Bun.spawn(["bash", "-n", caminho], { stdout: "pipe", stderr: "pipe" });
  const output = await new Response(proc.stderr).text();
  await proc.exited;
  return { ok: proc.exitCode === 0, erro: output.trim() };
}

async function validarPowerShell(caminho) {
  const cmd = `& { $tokens = $errors = $null; $ast = [System.Management.Automation.Language.Parser]::ParseFile('${caminho.replace(/'/g, "''")}', [ref]$tokens, [ref]$errors); if ($errors.Count -gt 0) { Write-Error $errors[0].Message; exit 1 } }`;
  const proc = Bun.spawn(["pwsh", "-NoProfile", "-NoLogo", "-Command", cmd], { stdout: "pipe", stderr: "pipe" });
  const output = await new Response(proc.stderr).text();
  await proc.exited;
  return { ok: proc.exitCode === 0, erro: output.trim() };
}

export default async () => ({
  "tool.execute.after": async (input, _output) => {
    if (input.tool !== "write" && input.tool !== "apply_patch") return;

    const caminho = input.args?.path || input.args?.filePath;
    if (!caminho || !ehArquivoInfra(caminho)) return;

    const avisos = [];
    const ext = caminho.split(".").pop()?.toLowerCase();

    if (ext === "sh") {
      const result = await validarBash(caminho);
      if (!result.ok)
        avisos.push(`ERRO CRÍTICO: Sintaxe bash inválida em ${caminho}. O script NÃO vai rodar.
  ${result.erro}`);
      else
        console.log(`[infra] ${caminho}: bash -n OK`);
    }

    if (ext === "ps1") {
      const result = await validarPowerShell(caminho);
      if (!result.ok)
        avisos.push(`ERRO CRÍTICO: Sintaxe PowerShell inválida em ${caminho}. O script NÃO vai rodar.
  ${result.erro}`);
      else
        console.log(`[infra] ${caminho}: pwsh parse OK`);
    }

    if (avisos.length > 0) {
      const feedback = [
        "========================================",
        " ATENÇÃO: SCRIPTS DE INFRA MODIFICADOS",
        "========================================",
        "",
        "Estes scripts são a porta de entrada do PMW. Um erro aqui impede",
        "instalação e atualização em TODAS as máquinas (Linux + Windows).",
        "",
        "Revise com atenção antes de commitar:",
        "  1. Leia infra/README.md com a checklist de revisão",
        "  2. Teste em VM/container Linux limpo",
        "  3. Teste em Windows limpo",
        "",
        ...avisos,
        "",
        "NÃO faça commit se houver ERRO CRÍTICO acima.",
        "Use o agente @infra-reviewer para revisão detalhada.",
        "========================================",
      ].join("\n");
      console.log(feedback);
    }
  }
});

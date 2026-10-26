import { spawnSync } from "node:child_process";

const ARQUIVOS_INFRA = [
  "bootstrap.sh",
  "bootstrap.ps1",
  "Atualizar_PMW.ps1",
  "infra/pmw.sh",
  "infra/pmw.ps1",
];

const normalizar = (caminho: string): string => caminho.replace(/\\/g, "/");

export function ehArquivoInfra(caminho: string): boolean {
  return ARQUIVOS_INFRA.some((arquivo) => normalizar(caminho).endsWith(arquivo));
}

function validarBash(caminho: string): string[] {
  const resultado = spawnSync("bash", ["-n", caminho], { encoding: "utf8" });

  if (resultado.error) return [];

  return resultado.status === 0
    ? []
    : [`ERRO CRÍTICO: sintaxe bash inválida. O script NÃO roda.\n${resultado.stderr.trim()}`];
}

function validarPowerShell(caminho: string): string[] {
  const script = [
    `$erros = $null`,
    `[System.Management.Automation.Language.Parser]::ParseFile('${caminho.replace(/'/g, "''")}', [ref]$null, [ref]$erros) | Out-Null`,
    `if ($erros.Count -gt 0) { Write-Error $erros[0].Message; exit 1 }`,
  ].join("; ");

  const resultado = spawnSync("pwsh", ["-NoProfile", "-NoLogo", "-Command", script], { encoding: "utf8" });

  if (resultado.error) return [];

  return resultado.status === 0
    ? []
    : [`ERRO CRÍTICO: sintaxe PowerShell inválida. O script NÃO roda.\n${resultado.stderr.trim()}`];
}

export function validarInfra(caminho: string): string[] {
  const extensao = caminho.split(".").pop()?.toLowerCase();

  if (extensao === "sh") return validarBash(caminho);
  if (extensao === "ps1") return validarPowerShell(caminho);

  return [];
}

# Hook preToolUse: bloqueia comandos git destrutivos e SQL destrutivo.
# Exit 2 = bloqueia execução. STDERR retorna motivo ao agente.
# Exit 0 = permite.

$input_json = [Console]::In.ReadToEnd()
$event = $input_json | ConvertFrom-Json

$toolName = $event.tool_name
if ($toolName -ne "shell" -and $toolName -ne "execute_bash" -and $toolName -ne "execute_cmd") { exit 0 }

$cmd = $event.tool_input.command
if (-not $cmd) { exit 0 }

# Git destrutivo
if ($cmd -match "git\s+(commit|push\s+--force|reset\s+--hard|clean\s+-f|branch\s+-D)") {
    [Console]::Error.WriteLine("BLOQUEADO: comando git destrutivo ('$cmd'). Peca autorizacao ao usuario.")
    exit 2
}

# SQL destrutivo
if ($cmd -match "(DROP\s+(TABLE|DATABASE|SCHEMA))|(DELETE\s+FROM)|(TRUNCATE)") {
    [Console]::Error.WriteLine("BLOQUEADO: comando SQL destrutivo ('$cmd'). Peca autorizacao ao usuario.")
    exit 2
}

exit 0

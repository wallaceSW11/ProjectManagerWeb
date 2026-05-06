# Hook postToolUse: valida code-style após escrita de arquivo.
# STDOUT retorna feedback ao agente para autocorreção.
# Exit 0 sempre (arquivo já foi salvo, apenas orienta o agente).

$input_json = [Console]::In.ReadToEnd()
$event = $input_json | ConvertFrom-Json

# Extrair path do arquivo salvo
$filePath = $null
if ($event.tool_input.path) {
    $filePath = $event.tool_input.path
} elseif ($event.tool_input.operations) {
    $filePath = ($event.tool_input.operations | Where-Object { $_.path } | Select-Object -First 1).path
}

if (-not $filePath -or -not (Test-Path $filePath)) { exit 0 }

$content = Get-Content $filePath -Raw -ErrorAction SilentlyContinue
if (-not $content) { exit 0 }

$violations = @()
$ext = [System.IO.Path]::GetExtension($filePath).ToLower()

# ===== BACKEND (.cs) =====
if ($ext -eq ".cs") {
    if ($content -match "AutoMapper|CreateMap|IMapper") {
        $violations += "PROIBIDO: AutoMapper detectado. Usar mapeamento manual no service."
    }
    if ($content -match "\.(Result|Wait\(\))") {
        $violations += "PROIBIDO: .Result/.Wait() detectado. Usar async/await."
    }
    if ($content -match "console\.log|Console\.WriteLine" -and $filePath -notmatch "Program\.cs") {
        $violations += "AVISO: Console.WriteLine detectado. Remover debug output."
    }
    if ($content -match "catch\s*\([^)]*\)\s*\{\s*\}") {
        $violations += "PROIBIDO: Exceção engolida (catch vazio). Sempre throw ou tratar."
    }
    if ($filePath -match "Controller\.cs$") {
        # Controller com lógica pesada (mais de orquestração)
        if ($content -match "File\.(ReadAll|WriteAll|Exists)|JsonSerializer\.(Serialize|Deserialize)") {
            $violations += "PROIBIDO: Controller acessando arquivo/JSON diretamente. Delegar para Service/JsonService."
        }
        if ($content -match "ActionResult<") {
            $violations += "AVISO: Usar IActionResult em vez de ActionResult<T> (padrão do projeto)."
        }
    }
    if ($content -match "public\s+class\s+\w+.*\n\s*\{\s*\n\s*(private|public|protected|internal)\s+readonly" -and $content -notmatch ":\s*(ControllerBase|Controller)") {
        # Heurística: classe com campo readonly no topo pode usar primary constructor
        if ($content -match "public\s+\w+\([^)]+\)\s*\{[\s\n]*_\w+\s*=") {
            $violations += "AVISO: Considerar primary constructor em vez de construtor tradicional com atribuição de campos."
        }
    }
}

# ===== FRONTEND (.vue, .ts) =====
if ($ext -eq ".vue" -or $ext -eq ".ts") {
    if ($content -match "\.then\s*\(") {
        $violations += "PROIBIDO: .then() detectado. Usar async/await."
    }
    if ($content -match "\bvar\s+\w+\s*[=;,:]") {
        $violations += "PROIBIDO: 'var' detectado. Usar const/let."
    }
    if ($content -match "console\.(log|warn|info|debug)") {
        $violations += "PROIBIDO: console.log detectado. Remover antes de entregar."
    }
    if ($content -match "!important") {
        $violations += "PROIBIDO: !important no CSS. Usar classes Vuetify."
    }
    if ($content -match "\bis[A-Z]\w*\s*[=:]|\bhas[A-Z]\w*\s*[=:]") {
        $violations += "AVISO: Prefixo is/has em booleano detectado. Usar nome sem prefixo (ex: loading, not isLoading)."
    }
}

if ($ext -eq ".vue") {
    # Detectar lógica no template
    $templateMatch = [regex]::Match($content, '<template>([\s\S]*?)</template>')
    if ($templateMatch.Success) {
        $template = $templateMatch.Groups[1].Value
        # Expressões complexas no template (operadores lógicos, ternários, .length, .filter, .map)
        if ($template -match 'v-if="[^"]*(\&\&|\|\||\.length|\.filter|\.map|\.find|\?.*:)') {
            $violations += "PROIBIDO: Lógica no template detectada (v-if com expressão complexa). Extrair para computed."
        }
        if ($template -match 'v-show="[^"]*(\&\&|\|\||\.length|\.filter|\.map|\.find|\?.*:)') {
            $violations += "PROIBIDO: Lógica no template detectada (v-show com expressão complexa). Extrair para computed."
        }
        if ($template -match ':class="[^"]*\?') {
            $violations += "PROIBIDO: Ternário em :class no template. Extrair para computed."
        }
    }
}

if ($ext -eq ".ts" -and $filePath -match "Store|store") {
    if ($content -match "loading\s*=\s*false" -and $content -notmatch "finally") {
        $violations += "AVISO: Loading sem finally detectado. Resetar loading no finally."
    }
}

# ===== OUTPUT =====
if ($violations.Count -gt 0) {
    $feedback = "CODE-STYLE: Violacoes encontradas em '$filePath'. Corrija AGORA:`n"
    $feedback += ($violations | ForEach-Object { "  - $_" }) -join "`n"
    $feedback += "`n`nReleia o arquivo, corrija as violacoes acima e salve novamente."
    Write-Output $feedback
}

exit 0

# ============================================================
# PMW - Project Manager Web - Bootstrap de instalação (Windows)
# ============================================================
# Uso (PowerShell 5+):
#
#   # Opção 1 — Baixar e executar direto:
#   powershell -ExecutionPolicy Bypass -Command "iwr -Uri 'https://raw.githubusercontent.com/wallaceSW11/ProjectManagerWeb/main/bootstrap.ps1' -OutFile '$env:TEMP\bootstrap.ps1'; & '$env:TEMP\bootstrap.ps1'"
#
#   # Opção 2 — Git clone + executar local:
#   git clone https://github.com/wallaceSW11/ProjectManagerWeb.git
#   .\ProjectManagerWeb\bootstrap.ps1 -Pasta "C:\inetpub\wwwroot\PMW"
# ============================================================

param(
    [string]$Pasta = "C:\inetpub\wwwroot\PMW"
)

$Repo     = "wallaceSW11/ProjectManagerWeb"
$ApiUrl   = "https://api.github.com/repos/$Repo/releases/latest"
$TempZip  = "$env:TEMP\PMW_install.zip"
$TempDir  = "$env:TEMP\PMW_install"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PMW - Project Manager Web" -ForegroundColor Cyan
Write-Host "  Instalação automatizada (Windows)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Pasta de destino: $Pasta"
Write-Host ""

# ------------------------------------------------------------------
# 1. Detecta sistema operacional
# ------------------------------------------------------------------
$isWin = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
if (-not $isWin) {
    Write-Host "❌ Este script é apenas para Windows." -ForegroundColor Red
    Write-Host "   Linux: use bootstrap.sh" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Sistema: Windows $([Environment]::OSVersion.Version)"
Write-Host ""

# ------------------------------------------------------------------
# 2. Obtém URL do último release
# ------------------------------------------------------------------
Write-Host "📡 Buscando último release do PMW..." -ForegroundColor Yellow
Write-Host "   $ApiUrl"

try {
    $release = Invoke-RestMethod -Uri $ApiUrl -Headers @{ "User-Agent" = "PMW-Bootstrap" }
} catch {
    Write-Host "❌ Falha ao buscar release: $_" -ForegroundColor Red
    Write-Host "   Verifique sua conexão com a internet."
    exit 1
}

$versao = $release.tag_name
$asset  = $release.assets | Where-Object { $_.name -like "PMW_Windows_*.zip" } | Select-Object -First 1

if (-not $asset) {
    Write-Host "❌ Nenhum artefato Windows encontrado no release $versao" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Release encontrado: $versao"
Write-Host "   Arquivo: $($asset.name)"
Write-Host ""

# ------------------------------------------------------------------
# 3. Baixa o release
# ------------------------------------------------------------------
Write-Host "📥 Baixando $versao ..." -ForegroundColor Yellow

try {
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $TempZip -Headers @{ "User-Agent" = "PMW-Bootstrap" }
} catch {
    Write-Host "❌ Falha ao baixar: $_" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Download concluído."
Write-Host ""

# ------------------------------------------------------------------
# 4. Extrai para a pasta de destino
# ------------------------------------------------------------------
Write-Host "📂 Extraindo para $Pasta ..." -ForegroundColor Yellow

if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
Expand-Archive -Path $TempZip -DestinationPath $TempDir -Force

if (-not (Test-Path $Pasta)) { New-Item -ItemType Directory -Path $Pasta -Force | Out-Null }

# Copia tudo (preserva appsettings.json se já existir)
if (Test-Path (Join-Path $Pasta "appsettings.json")) {
    Get-ChildItem -Path $TempDir | Where-Object { $_.Name -ne "appsettings.json" } | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $Pasta -Recurse -Force
    }
} else {
    Get-ChildItem -Path $TempDir | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $Pasta -Recurse -Force
    }
}

Write-Host "✅ Extraído com sucesso."
Write-Host ""

# ------------------------------------------------------------------
# 5. Configura infraestrutura
# ------------------------------------------------------------------
Write-Host "🔧 Configurando infraestrutura..." -ForegroundColor Yellow

$infraScript = Join-Path $Pasta "infra\pmw.ps1"
if (Test-Path $infraScript) {
    # Executa o install do pmw.ps1 (copia scripts para C:\PMW-Tools, adiciona ao PATH)
    & $infraScript install -Pasta $Pasta
} else {
    Write-Host "⚠️  Script de infra não encontrado em $infraScript" -ForegroundColor Yellow
    Write-Host "   Configuração manual necessária."
}

# ------------------------------------------------------------------
# 6. Limpa temporários
# ------------------------------------------------------------------
Remove-Item $TempZip -Force -ErrorAction SilentlyContinue
Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue

# ------------------------------------------------------------------
# 7. Mensagem final
# ------------------------------------------------------------------
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✅ PMW instalado com sucesso!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "   📍 Aplicação:  $Pasta"
Write-Host "   📍 Scripts:    C:\PMW-Tools"
Write-Host "   🔗 Comando:    pmw"
Write-Host ""
Write-Host "   Para iniciar:"
Write-Host "     pmw start"
Write-Host ""
Write-Host "   Acesse: http://localhost:2025"
Write-Host ""
Write-Host "   Para ver o status:"
Write-Host "     pmw status"
Write-Host ""
Write-Host "   Para atualizar:"
Write-Host "     pmw update"
Write-Host ""
Write-Host "========================================"

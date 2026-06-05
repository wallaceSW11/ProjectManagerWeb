# ============================================================
# Atualizar_PMW.ps1
# Baixa o último release do GitHub e instala na pasta informada
# Com backup automático e preservação de appsettings.json
#
# Uso:
#   .\Atualizar_PMW.ps1
#   .\Atualizar_PMW.ps1 -Pasta "C:\inetpub\wwwroot\PMW_2"
# ============================================================

param(
    [string]$Pasta = "C:\inetpub\wwwroot\PMW"
)

$Repo     = "wallaceSW11/ProjectManagerWeb"
$ApiUrl   = "https://api.github.com/repos/$Repo/releases/latest"
$TempZip  = "$env:TEMP\PMW_update.zip"
$TempDir  = "$env:TEMP\PMW_update"

Write-Host ""
Write-Host "=== Atualizador PMW ===" -ForegroundColor Cyan
Write-Host "Pasta de destino: $Pasta"
Write-Host ""

# --- 1. Buscar último release ---
Write-Host "Buscando último release..." -ForegroundColor Yellow
try {
    $release = Invoke-RestMethod -Uri $ApiUrl -Headers @{ "User-Agent" = "PMW-Updater" }
} catch {
    Write-Host "Falha ao buscar release: $_" -ForegroundColor Red
    exit 1
}

$versao   = $release.tag_name
$asset    = $release.assets | Where-Object { $_.name -like "PMW_Windows_*.zip" } | Select-Object -First 1

if (-not $asset) {
    Write-Host "Nenhum artefato Windows encontrado no release $versao" -ForegroundColor Red
    exit 1
}

Write-Host "Versão disponível: $versao" -ForegroundColor Green
Write-Host "Arquivo: $($asset.name)"
Write-Host ""

# --- 2. Backup com data/hora ---
if (Test-Path $Pasta) {
    $dataHora = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupDir = "C:\PMW-backup-$dataHora"
    Write-Host "Criando backup em $backupDir ..." -ForegroundColor Yellow
    Copy-Item -Path $Pasta -Destination $backupDir -Recurse -Force
    Write-Host "Backup concluído."
}

# --- 3. Parar processo ---
Write-Host "Parando processo PMW..." -ForegroundColor Yellow
Stop-Process -Name "ProjectManagerWeb" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# --- 4. Baixar zip ---
Write-Host "Baixando $($asset.name)..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $TempZip -Headers @{ "User-Agent" = "PMW-Updater" }
} catch {
    Write-Host "Falha ao baixar: $_" -ForegroundColor Red
    exit 1
}

# --- 5. Extrair ---
Write-Host "Extraindo arquivos..." -ForegroundColor Yellow
if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
Expand-Archive -Path $TempZip -DestinationPath $TempDir -Force

# --- 6. Copiar para pasta de destino (preserva appsettings.json local) ---
Write-Host "Instalando em $Pasta..." -ForegroundColor Yellow
if (-not (Test-Path $Pasta)) { New-Item -ItemType Directory -Path $Pasta -Force | Out-Null }

Get-ChildItem -Path $TempDir | Where-Object { $_.Name -ne "appsettings.json" } | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $Pasta -Recurse -Force
}

# --- 7. Copiar infra para C:\inetpub\wwwroot\PMW-Tools (se existir no pacote) ---
$infraDir = Join-Path $TempDir "infra"
if (Test-Path $infraDir) {
    $toolsDir = "C:\inetpub\wwwroot\PMW-Tools"
    if (-not (Test-Path $toolsDir)) { New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null }
    Copy-Item "$infraDir\pmw.ps1" $toolsDir -Force
    Copy-Item "$infraDir\pmw-start.vbs" $toolsDir -Force
    Copy-Item "$infraDir\pmw-start.bat" $toolsDir -Force
    Remove-Item "$Pasta\infra" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Scripts de infra copiados para $toolsDir"
}

# --- 8. Iniciar processo ---
Write-Host "Iniciando PMW..." -ForegroundColor Yellow
$exe = Join-Path $Pasta "ProjectManagerWeb.exe"
if (Test-Path $exe) {
    # Novo layout: executável na raiz
    Start-Process -FilePath $exe -WindowStyle Hidden -WorkingDirectory $Pasta
} else {
    # Fallback: versão antiga (dentro de backend\)
    $exe = Join-Path $Pasta "backend\ProjectManagerWeb.exe"
    if (Test-Path $exe) {
        Start-Process -FilePath $exe -WindowStyle Hidden -WorkingDirectory (Join-Path $Pasta "backend")
    }
}

# --- 9. Limpar temporários ---
Remove-Item $TempZip -Force -ErrorAction SilentlyContinue
Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "✅ PMW $versao instalado com sucesso em $Pasta" -ForegroundColor Green
Write-Host ""
Write-Host "Comandos:"
Write-Host "  C:\inetpub\wwwroot\PMW-Tools\pmw.ps1 {start|stop|restart|status|update}"
Write-Host "  (adicione C:\inetpub\wwwroot\PMW-Tools ao PATH para usar só 'pmw')"

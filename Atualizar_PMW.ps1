# ============================================================
# Atualizar_PMW.ps1
# Baixa o ultimo release do GitHub e instala na pasta informada
#
# Uso:
#   .\Atualizar_PMW.ps1
#   .\Atualizar_PMW.ps1 -Pasta "C:\inetpub\wwwroot\PMW_2"
# ============================================================

param(
    [string]$Pasta = "C:\inetpub\wwwroot\PMW"
)

$Repo     = "wallacesw11/ProjectManagerWeb"
$ApiUrl   = "https://api.github.com/repos/$Repo/releases/latest"
$TempZip  = "$env:TEMP\PMW_update.zip"
$TempDir  = "$env:TEMP\PMW_update"

Write-Host ""
Write-Host "=== Atualizador PMW ===" -ForegroundColor Cyan
Write-Host "Pasta de destino: $Pasta"
Write-Host ""

# --- 1. Buscar ultimo release ---
Write-Host "Buscando ultimo release..." -ForegroundColor Yellow
try {
    $release = Invoke-RestMethod -Uri $ApiUrl -Headers @{ "User-Agent" = "PMW-Updater" }
} catch {
    Write-Host "Falha ao buscar release: $_" -ForegroundColor Red
    exit 1
}

$versao   = $release.tag_name
$asset    = $release.assets | Where-Object { $_.name -like "PMW_*.zip" } | Select-Object -First 1

if (-not $asset) {
    Write-Host "Nenhum artefato encontrado no release $versao" -ForegroundColor Red
    exit 1
}

Write-Host "Versao disponivel: $versao" -ForegroundColor Green
Write-Host "Arquivo: $($asset.name)"
Write-Host ""

# --- 2. Parar processo ---
Write-Host "Parando processo PMW..." -ForegroundColor Yellow
Stop-Process -Name "ProjectManagerWeb" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# --- 3. Baixar zip ---
Write-Host "Baixando $($asset.name)..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $TempZip -Headers @{ "User-Agent" = "PMW-Updater" }
} catch {
    Write-Host "Falha ao baixar: $_" -ForegroundColor Red
    exit 1
}

# --- 4. Extrair ---
Write-Host "Extraindo arquivos..." -ForegroundColor Yellow
if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
Expand-Archive -Path $TempZip -DestinationPath $TempDir -Force

# --- 5. Copiar para pasta de destino (preserva appsettings.json local) ---
Write-Host "Instalando em $Pasta..." -ForegroundColor Yellow
if (-not (Test-Path $Pasta)) { New-Item -ItemType Directory -Path $Pasta | Out-Null }

Get-ChildItem -Path $TempDir | Where-Object { $_.Name -ne "appsettings.json" } | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $Pasta -Recurse -Force
}

# --- 6. Iniciar processo ---
Write-Host "Iniciando PMW..." -ForegroundColor Yellow
$exe = Join-Path $Pasta "ProjectManagerWeb.exe"
Start-Process -FilePath $exe -ArgumentList "start" -WindowStyle Hidden -WorkingDirectory $Pasta

# --- 7. Limpar temporarios ---
Remove-Item $TempZip -Force -ErrorAction SilentlyContinue
Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "PMW $versao instalado com sucesso em $Pasta" -ForegroundColor Green
Write-Host ""

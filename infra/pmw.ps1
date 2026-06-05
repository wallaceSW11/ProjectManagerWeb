# ============================================================
# PMW - Project Manager Web - Script de gerenciamento (Windows)
# ============================================================
# Diretórios:
#   C:\inetpub\wwwroot\PMW → aplicação (default)
#   C:\inetpub\wwwroot\PMW-Tools → scripts de infra (pmw.ps1)
# ============================================================

param(
    [string]$Comando = "help",
    [string]$Pasta = ""    # opcional: diretório da aplicação
)

$PMW_TOOLS = "C:\inetpub\wwwroot\PMW-Tools"
$CONFIG_FILE = Join-Path $PMW_TOOLS "pmw-dir.txt"
$REPO      = "wallaceSW11/ProjectManagerWeb"

# Detecta o diretório da aplicação
function Get-PmwDir {
    # 1. Se foi passado como parâmetro, salva e usa
    if ($Pasta -and (Test-Path $Pasta)) {
        $Pasta | Out-File -FilePath $CONFIG_FILE -Encoding utf8 -Force
        return $Pasta
    }

    # 2. Tenta ler do arquivo de configuração
    if (Test-Path $CONFIG_FILE) {
        $dir = Get-Content $CONFIG_FILE -Raw | ForEach-Object { $_.Trim() }
        if ($dir -and (Test-Path $dir)) { return $dir }
    }

    # 3. Tenta detectar em locais comuns
    $candidatos = @(
        "C:\inetpub\wwwroot\PMW",
        "C:\PMW",
        "C:\wwwroot\PMW",
        "$env:USERPROFILE\PMW"
    )
    foreach ($c in $candidatos) {
        # Novo layout: executável na raiz
        if (Test-Path (Join-Path $c "ProjectManagerWeb.exe")) {
            $c | Out-File -FilePath $CONFIG_FILE -Encoding utf8 -Force
            return $c
        }
        # Fallback: layout antigo (dentro de backend\)
        if (Test-Path (Join-Path $c "backend\ProjectManagerWeb.exe")) {
            $c | Out-File -FilePath $CONFIG_FILE -Encoding utf8 -Force
            return $c
        }
    }

    # 4. Não encontrou
    Write-Host "❌ PMW não encontrado. Informe o caminho:" -ForegroundColor Red
    Write-Host "   .\pmw.ps1 start -Pasta ""C:\inetpub\wwwroot\PMW""" -ForegroundColor Yellow
    return $null
}

$PMW_DIR = Get-PmwDir

function Start-PMW {
    $dir = Get-PmwDir
    if (-not $dir) { return }
    $exe = Join-Path $dir "ProjectManagerWeb.exe"
    if (-not (Test-Path $exe)) {
        Write-Host "❌ executável não encontrado em $dir" -ForegroundColor Red
        return
    }
    $proc = Get-Process -Name "ProjectManagerWeb" -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Host "⚠️  PMW já está em execução (PID $($proc.Id))" -ForegroundColor Yellow
        return
    }
    Start-Process -FilePath $exe -WindowStyle Hidden -WorkingDirectory $dir
    Write-Host "✅ PMW iniciado. Acesse http://localhost:2025" -ForegroundColor Green
}

function Stop-PMW {
    $proc = Get-Process -Name "ProjectManagerWeb" -ErrorAction SilentlyContinue
    if (-not $proc) {
        Write-Host "⚠️  PMW não está em execução" -ForegroundColor Yellow
        return
    }
    Stop-Process -Name "ProjectManagerWeb" -Force
    Write-Host "✅ PMW parado." -ForegroundColor Green
}

function Restart-PMW {
    Stop-PMW
    Start-Sleep -Seconds 2
    Start-PMW
}

function Status-PMW {
    $dir = Get-PmwDir
    $proc = Get-Process -Name "ProjectManagerWeb" -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Host "✅ PMW em execução (PID $($proc.Id))" -ForegroundColor Green
        Write-Host "   Acesse: http://localhost:2025"
    } else {
        Write-Host "❌ PMW não está em execução" -ForegroundColor Red
    }
}

function Install-PMW {
    Write-Host "🔧 Configurando infraestrutura do PMW..." -ForegroundColor Cyan

    $dir = Get-PmwDir
    if (-not $dir) {
        Write-Host "   Informe o diretório da aplicação:" -ForegroundColor Yellow
        Write-Host "   .\pmw.ps1 install -Pasta ""C:\inetpub\wwwroot\PMW""" -ForegroundColor Yellow
        return
    }

    # Cria o diretório da aplicação se não existir
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "   Diretório criado: $dir"
    }

    if (-not (Test-Path $PMW_TOOLS)) { New-Item -ItemType Directory -Path $PMW_TOOLS -Force | Out-Null }

    # Copia scripts de infra para PMW_TOOLS
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    Copy-Item "$scriptDir\pmw.ps1" $PMW_TOOLS -Force
    Copy-Item "$scriptDir\pmw-start.vbs" $PMW_TOOLS -Force
    Copy-Item "$scriptDir\pmw-start.bat" $PMW_TOOLS -Force
    Write-Host "   Scripts copiados para $PMW_TOOLS"

    # Salva o diretório da aplicação
    $dir | Out-File -FilePath $CONFIG_FILE -Encoding utf8 -Force

    # Adiciona ao PATH do usuário
    $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($userPath -notlike "*$PMW_TOOLS*") {
        [Environment]::SetEnvironmentVariable("Path", "$userPath;$PMW_TOOLS", "User")
        Write-Host "   Diretório $PMW_TOOLS adicionado ao PATH do usuário"
        Write-Host "   Reabra o terminal para usar o comando 'pmw'"
    }

    Write-Host ""
    Write-Host "✅ PMW instalado em $dir" -ForegroundColor Green
    Write-Host "   Scripts de infra em $PMW_TOOLS"
    Write-Host ""
    Write-Host "   Use 'pmw start' para iniciar."
    Write-Host "   Ou: powershell -File "$PMW_TOOLS\pmw.ps1" start"
}

function Update-PMW {
    Write-Host "🔄 Atualizando PMW..." -ForegroundColor Cyan

    $dir = Get-PmwDir
    if (-not $dir) { return }

    # Para o processo
    $proc = Get-Process -Name "ProjectManagerWeb" -ErrorAction SilentlyContinue
    if ($proc) {
        Stop-Process -Name "ProjectManagerWeb" -Force
        Write-Host "   PMW parado."
        Start-Sleep -Seconds 2
    }

    # Backup com data/hora
    $dataHora = Get-Date -Format "yyyyMMdd_HHmmss"
    $nomeDir = Split-Path $dir -Leaf
    $backupDir = "C:\PMW-backup-$nomeDir-$dataHora"
    if (Test-Path $dir) {
        Write-Host "   Criando backup em $backupDir ..."
        Copy-Item -Path $dir -Destination $backupDir -Recurse -Force
        Write-Host "   Backup concluído."
    }

    # Buscar último release
    $apiUrl = "https://api.github.com/repos/$REPO/releases/latest"
    Write-Host "   Buscando último release..."
    try {
        $release = Invoke-RestMethod -Uri $apiUrl -Headers @{ "User-Agent" = "PMW-Updater" }
    } catch {
        Write-Host "❌ Falha ao buscar release: $_" -ForegroundColor Red
        exit 1
    }

    $versao = $release.tag_name
    $asset = $release.assets | Where-Object { $_.name -like "PMW_Windows_*.zip" } | Select-Object -First 1

    if (-not $asset) {
        Write-Host "❌ Nenhum artefato Windows encontrado no release $versao" -ForegroundColor Red
        exit 1
    }

    Write-Host "   Versão: $versao"
    Write-Host "   Arquivo: $($asset.name)"

    # Baixar
    $tempZip = "$env:TEMP\PMW_update.zip"
    Write-Host "   Baixando..."
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $tempZip -Headers @{ "User-Agent" = "PMW-Updater" }

    # Extrair
    $tempDir = "$env:TEMP\PMW_update"
    if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
    Expand-Archive -Path $tempZip -DestinationPath $tempDir -Force

    # Copiar para destino (preserva appsettings.json)
    Write-Host "   Instalando em $dir..."
    Get-ChildItem -Path $tempDir | Where-Object { $_.Name -ne "appsettings.json" } | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $dir -Recurse -Force
    }

    # Atualiza scripts de infra
    $infraDir = Join-Path $tempDir "infra"
    if (Test-Path $infraDir) {
        if (-not (Test-Path $PMW_TOOLS)) { New-Item -ItemType Directory -Path $PMW_TOOLS -Force | Out-Null }
        Copy-Item "$infraDir\pmw.ps1" $PMW_TOOLS -Force
        Copy-Item "$infraDir\pmw-start.vbs" $PMW_TOOLS -Force
        Copy-Item "$infraDir\pmw-start.bat" $PMW_TOOLS -Force
        Remove-Item "$dir\infra" -Recurse -Force -ErrorAction SilentlyContinue
    }

    # Limpar
    Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host "✅ Atualização concluída." -ForegroundColor Green

    # Iniciar
    Start-PMW
}

# ============================================================
switch ($Comando.ToLower()) {
    "start"   { Start-PMW }
    "stop"    { Stop-PMW }
    "restart" { Restart-PMW }
    "status"  { Status-PMW }
    "install" { Install-PMW }
    "update"  { Update-PMW }
    default {
        Write-Host ""
        Write-Host "PMW - Project Manager Web" -ForegroundColor Cyan
        Write-Host "Uso: pmw {comando}" -ForegroundColor White
        Write-Host ""
        Write-Host "   start     Inicia o serviço"
        Write-Host "   stop      Para o serviço"
        Write-Host "   restart   Reinicia o serviço"
        Write-Host "   status    Status do serviço"
        Write-Host "   install   Configura a infraestrutura"
        Write-Host "   update    Atualiza para o último release (com backup)"
        Write-Host ""
    }
}

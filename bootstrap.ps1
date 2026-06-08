# PMW - Project Manager Web - Bootstrap de instalação (Windows)
# Uso (PowerShell como administrador):
#   irm https://raw.githubusercontent.com/wallaceSW11/ProjectManagerWeb/main/bootstrap.ps1 -OutFile "$env:TEMP\bootstrap.ps1"; & "$env:TEMP\bootstrap.ps1"
#
# Parâmetros opcionais:
#   .\bootstrap.ps1 -Pasta "C:\inetpub\wwwroot\PMW"

param(
    [string]$Pasta = "C:\inetpub\wwwroot\PMW"
)

$Repo       = "wallaceSW11/ProjectManagerWeb"
$PastaPai   = Split-Path $Pasta -Parent
$PMW_TOOLS  = Join-Path $PastaPai "PMW-Tools"
$PMW_BKPS   = Join-Path $PastaPai "PMW-Bkps"
$ApiUrl     = "https://api.github.com/repos/$Repo/releases/latest"
$TempZip    = "$env:TEMP\PMW_install.zip"
$TempDir    = "$env:TEMP\PMW_install"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PMW - Project Manager Web" -ForegroundColor Cyan
Write-Host "  Instalação automatizada (Windows)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ------------------------------------------------------------------
# 1. Detecta sistema operacional
# ------------------------------------------------------------------
if ($env:OS -notlike "*Windows*") {
    Write-Host "Este script é apenas para Windows." -ForegroundColor Red
    Write-Host "Linux: use o bootstrap.sh" -ForegroundColor Yellow
    exit 1
}

Write-Host "Sistema: Windows $([Environment]::OSVersion.Version)"

# ------------------------------------------------------------------
# 2. Verifica elevação (admin)
# ------------------------------------------------------------------
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    $driveRoot = [System.IO.Path]::GetPathRoot($Pasta).TrimEnd('\')
    $systemDrive = [Environment]::GetEnvironmentVariable("SystemDrive").TrimEnd('\')
    if ($driveRoot -eq $systemDrive) {
        Write-Host "A instalação em $Pasta requer privilégios de administrador." -ForegroundColor Yellow
        Write-Host "Reiniciando como administrador..." -ForegroundColor Yellow
        Start-Sleep -Seconds 2

        $scriptPath = $MyInvocation.MyCommand.Path
        if (-not $scriptPath) {
            $scriptPath = Join-Path $env:TEMP "bootstrap.ps1"
            $url = "https://raw.githubusercontent.com/$Repo/main/bootstrap.ps1"
            Invoke-WebRequest -Uri $url -OutFile $scriptPath -Headers @{ "User-Agent" = "PMW-Bootstrap" }
        }

        $argList = "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`" -Pasta `"$Pasta`""
        Start-Process -FilePath "powershell" -ArgumentList $argList -Verb RunAs
        exit 0
    }
}

Write-Host ""

# ------------------------------------------------------------------
# 3. Verifica se já existe instalação
# ------------------------------------------------------------------
$instalacaoExistente = Test-Path (Join-Path $Pasta "ProjectManagerWeb.exe")

if ($instalacaoExistente) {
    Write-Host "PMW já está instalado em $Pasta" -ForegroundColor Yellow
    $resposta = Read-Host "Deseja atualizar para o último release? (S/n)"
    if ($resposta -ne "" -and $resposta -ne "s" -and $resposta -ne "S") {
        Write-Host "Instalação cancelada." -ForegroundColor Red
        exit 0
    }
    Write-Host ""
    Write-Host "Atualizando PMW..." -ForegroundColor Cyan

    # Para o processo se estiver rodando
    $proc = Get-Process -Name "ProjectManagerWeb" -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Host "Parando PMW em execução..." -ForegroundColor Yellow
        Stop-Process -Name "ProjectManagerWeb" -Force
        Start-Sleep -Seconds 2
    }

    # Backup com data/hora
    $dataHora = Get-Date -Format "yyyyMMdd_HHmmss"
    if (-not (Test-Path $PMW_BKPS)) { New-Item -ItemType Directory -Path $PMW_BKPS -Force | Out-Null }
    $backupDir = Join-Path $PMW_BKPS $dataHora
    $appBkp = Join-Path $backupDir "app"
    $bancoBkp = Join-Path $backupDir "banco"

    Write-Host "Criando backup da aplicação em $appBkp ..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $appBkp -Force | Out-Null
    Copy-Item -Path "$Pasta\*" -Destination $appBkp -Recurse -Force

    $bancoOrigem = Join-Path $env:APPDATA "PMW\Banco"
    if (Test-Path $bancoOrigem) {
        Write-Host "Criando backup do banco em $bancoBkp ..." -ForegroundColor Yellow
        New-Item -ItemType Directory -Path $bancoBkp -Force | Out-Null
        Copy-Item -Path "$bancoOrigem\*" -Destination $bancoBkp -Recurse -Force
    }

    Write-Host "Backup concluído em $backupDir" -ForegroundColor Green
    Write-Host ""
}

# ------------------------------------------------------------------
# 4. Busca último release
# ------------------------------------------------------------------
Write-Host "Buscando último release do PMW..." -ForegroundColor Yellow
Write-Host "   $ApiUrl"

try {
    $release = Invoke-RestMethod -Uri $ApiUrl -Headers @{ "User-Agent" = "PMW-Bootstrap" }
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

Write-Host "Release encontrado: $versao"
Write-Host ""

# ------------------------------------------------------------------
# 5. Baixa o zip
# ------------------------------------------------------------------
Write-Host "Baixando $($asset.name) ..." -ForegroundColor Yellow

try {
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $TempZip -Headers @{ "User-Agent" = "PMW-Bootstrap" }
} catch {
    Write-Host "Falha ao baixar: $_" -ForegroundColor Red
    exit 1
}

# ------------------------------------------------------------------
# 6. Extrai para o diretório de instalação
# ------------------------------------------------------------------
Write-Host "Extraindo para $Pasta ..." -ForegroundColor Yellow

if (-not (Test-Path $Pasta)) {
    New-Item -ItemType Directory -Path $Pasta -Force | Out-Null
}

if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
Expand-Archive -Path $TempZip -DestinationPath $TempDir -Force

# Copia tudo (preserva appsettings.json se já existir localmente)
Get-ChildItem -Path $TempDir | Where-Object { $_.Name -ne "appsettings.json" } | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $Pasta -Recurse -Force
}

Remove-Item $TempZip -Force -ErrorAction SilentlyContinue
Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""

# ------------------------------------------------------------------
# 7. Configura infraestrutura
# ------------------------------------------------------------------
Write-Host "Configurando infraestrutura..." -ForegroundColor Yellow

if (-not (Test-Path $PMW_BKPS)) { New-Item -ItemType Directory -Path $PMW_BKPS -Force | Out-Null }

$infraScript = Join-Path $Pasta "infra\pmw.ps1"

if (-not (Test-Path $infraScript)) {
    Write-Host "Release antigo detectado. Configuração manual necessária:" -ForegroundColor Yellow
    Write-Host "   1. Abra PowerShell como administrador" -ForegroundColor Yellow
    Write-Host "   2. Execute: $infraScript install -Pasta `"$Pasta`"" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "PMW $versao instalado em $Pasta" -ForegroundColor Green
    Write-Host "Acesse http://localhost:2025" -ForegroundColor Green
    exit 0
}

# Executa o install do pmw.ps1
& $infraScript install -Pasta $Pasta

# ------------------------------------------------------------------
# 8. Inicia o PMW
# ------------------------------------------------------------------
Write-Host ""
Write-Host "Iniciando PMW..." -ForegroundColor Yellow

$toolsScript = Join-Path $PMW_TOOLS "pmw.ps1"
if (Test-Path $toolsScript) {
    & $toolsScript start
} else {
    & $infraScript start
}

# ------------------------------------------------------------------
# 9. Mensagem final
# ------------------------------------------------------------------
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PMW instalado e iniciado com sucesso!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Aplicação:  $Pasta" -ForegroundColor White
Write-Host "   Scripts:    $PMW_TOOLS" -ForegroundColor White
Write-Host "   Backups:    $PMW_BKPS" -ForegroundColor White
Write-Host "   Comando:    pmw" -ForegroundColor White
Write-Host ""
Write-Host "   Acesse: http://localhost:2025" -ForegroundColor Green
Write-Host ""
Write-Host "   Para parar:          pmw stop" -ForegroundColor Yellow
Write-Host "   Para iniciar:        pmw start" -ForegroundColor Yellow
Write-Host "   Para atualizar:      pmw update" -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

# Migration: Adicionar campo "IDEIdentificador" aos repositórios existentes
# Data: 2026-01-09
# Descrição: Adiciona o campo "ideIdentificador" com a primeira IDE disponível como padrão

$ErrorActionPreference = "Stop"

$caminhoRepositorios = "$env:ProgramData\PMW\Banco\repositorios.json"
$caminhoIDEs = "$env:ProgramData\PMW\Banco\ides.json"

Write-Host "=== Migration: Adicionar campo 'IDEIdentificador' aos repositórios ===" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $caminhoRepositorios)) {
    Write-Host "Arquivo não encontrado: $caminhoRepositorios" -ForegroundColor Red
    Write-Host "Nenhuma ação necessária - banco vazio" -ForegroundColor Yellow
    exit 0
}

if (-not (Test-Path $caminhoIDEs)) {
    Write-Host "Arquivo de IDEs não encontrado: $caminhoIDEs" -ForegroundColor Red
    Write-Host "Nenhuma IDE disponível para configurar" -ForegroundColor Yellow
    exit 0
}

Write-Host "Lendo arquivos..." -ForegroundColor Gray

$backup = "$caminhoRepositorios.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $caminhoRepositorios $backup
Write-Host "Backup criado: $backup" -ForegroundColor Green

$jsonRepositorios = Get-Content $caminhoRepositorios -Raw -Encoding UTF8
$repositorios = $jsonRepositorios | ConvertFrom-Json

$jsonIDEs = Get-Content $caminhoIDEs -Raw -Encoding UTF8
$ides = $jsonIDEs | ConvertFrom-Json

if ($ides.Count -eq 0) {
    Write-Host "Nenhuma IDE cadastrada no sistema" -ForegroundColor Yellow
    exit 0
}

$primeiraIDE = $ides[0].identificador
Write-Host "IDE padrão selecionada: $($ides[0].nome) ($primeiraIDE)" -ForegroundColor Cyan

$totalRepositorios = $repositorios.Count
$totalAtualizados = 0

Write-Host ""
Write-Host "Processando $totalRepositorios repositório(s)..." -ForegroundColor Cyan

foreach ($repo in $repositorios) {
    if (-not $repo.PSObject.Properties['ideIdentificador'] -or $null -eq $repo.ideIdentificador) {
        if ($repo.PSObject.Properties['ideIdentificador']) {
            $repo.ideIdentificador = $primeiraIDE
        } else {
            $repo | Add-Member -MemberType NoteProperty -Name 'ideIdentificador' -Value $primeiraIDE
        }
        $totalAtualizados++
    }
}

$jsonAtualizado = $repositorios | ConvertTo-Json -Depth 10 -Compress:$false
[System.IO.File]::WriteAllText($caminhoRepositorios, $jsonAtualizado, [System.Text.Encoding]::UTF8)

Write-Host ""
Write-Host "=== Migration concluída com sucesso! ===" -ForegroundColor Green
Write-Host "Total de repositórios atualizados: $totalAtualizados" -ForegroundColor Cyan
Write-Host "IDE padrão configurada: $($ides[0].nome)" -ForegroundColor Cyan
Write-Host "Backup disponível em: $backup" -ForegroundColor Gray
Write-Host ""

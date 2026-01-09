# Migration: Adicionar campo "Ativo" aos menus existentes
# Data: 2026-01-09
# Descrição: Adiciona o campo "ativo" com valor true em todos os menus dos repositórios

$ErrorActionPreference = "Stop"

$caminhoArquivo = "$env:ProgramData\PMW\Banco\repositorios.json"

Write-Host "=== Migration: Adicionar campo 'Ativo' aos menus ===" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $caminhoArquivo)) {
    Write-Host "Arquivo não encontrado: $caminhoArquivo" -ForegroundColor Red
    Write-Host "Nenhuma ação necessária - banco vazio" -ForegroundColor Yellow
    exit 0
}

Write-Host "Lendo arquivo: $caminhoArquivo" -ForegroundColor Gray

$backup = "$caminhoArquivo.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $caminhoArquivo $backup
Write-Host "Backup criado: $backup" -ForegroundColor Green

$json = Get-Content $caminhoArquivo -Raw -Encoding UTF8
$repositorios = $json | ConvertFrom-Json

$totalRepositorios = $repositorios.Count
$totalMenusAtualizados = 0

Write-Host ""
Write-Host "Processando $totalRepositorios repositório(s)..." -ForegroundColor Cyan

foreach ($repo in $repositorios) {
    if ($repo.menus -and $repo.menus.Count -gt 0) {
        foreach ($menu in $repo.menus) {
            if (-not $menu.PSObject.Properties['ativo']) {
                $menu | Add-Member -MemberType NoteProperty -Name 'ativo' -Value $true
                $totalMenusAtualizados++
            }
        }
    }
}

$jsonAtualizado = $repositorios | ConvertTo-Json -Depth 10 -Compress:$false
[System.IO.File]::WriteAllText($caminhoArquivo, $jsonAtualizado, [System.Text.Encoding]::UTF8)

Write-Host ""
Write-Host "=== Migration concluída com sucesso! ===" -ForegroundColor Green
Write-Host "Total de menus atualizados: $totalMenusAtualizados" -ForegroundColor Cyan
Write-Host "Backup disponível em: $backup" -ForegroundColor Gray
Write-Host ""

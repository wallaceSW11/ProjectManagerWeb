# Isso é um comentário no PowerShell

# Equivalente a "taskkill /IM ProjectManagerWeb.exe /F 2>nul"
# O -ErrorAction SilentlyContinue faz o mesmo que "2>nul" (ignora erros se o processo não existir)
Stop-Process -Name "ProjectManagerWeb" -Force -ErrorAction SilentlyContinue

# Equivalente a "cd C:\tools\PMW\ProjectManagerWeb"
Set-Location "C:\tools\PMW\ProjectManagerWeb"

# Comandos git e npm funcionam da mesma forma
git reset --hard origin/main
git pull

# Equivalente a "cd frontend"
Set-Location "frontend"
npm run publish:all

# Equivalente a "cd C:\inetpub\wwwroot\PMW"
Set-Location "C:\inetpub\wwwroot\PMW"

# O comando para iniciar o processo, agora de forma nativa
Start-Process -FilePath "ProjectManagerWeb.exe" -ArgumentList "start" -WindowStyle Hidden

Write-Host "Script PowerShell concluído com sucesso."
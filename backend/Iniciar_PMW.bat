@echo off
cd /d C:\inetpub\wwwroot\PMW

REM Mata o processo se estiver rodando
taskkill /IM ProjectManagerWeb.exe /F 2>nul

REM Inicia o exe em segundo plano via PowerShell
pwsh -NoProfile -Command "Start-Process 'ProjectManagerWeb.exe' -ArgumentList 'start' -WindowStyle Hidden"

REM Fecha o CMD
exit

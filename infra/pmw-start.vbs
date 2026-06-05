' ============================================================
' PMW - Inicia sem janela de terminal
' ============================================================
' Esse script VBScript inicia o PMW sem exibir console.
' Ideal para colocar na inicialização do Windows.
'
' Uso:
'   wscript.exe "C:\inetpub\wwwroot\PMW-Tools\pmw-start.vbs"
' ============================================================

Dim shell, pmwTools, psCmd

pmwTools = "C:\inetpub\wwwroot\PMW-Tools"
psCmd = "powershell -ExecutionPolicy Bypass -NoProfile -File """ & pmwTools & "\pmw.ps1"" start"

Set shell = CreateObject("WScript.Shell")

' 0 = hide window, False = don't wait
shell.Run psCmd, 0, False

Set shell = Nothing

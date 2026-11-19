@echo off
setlocal
set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
set "OUT=%~dp0Kateb.exe"
set "ICON=%~dp0assets\icon.ico"

"%CSC%" /nologo /target:winexe /out:"%OUT%" /win32icon:"%ICON%" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "%~dp0src\ImageTextMaker.cs" "%~dp0src\UiControls.cs" "%~dp0src\MainFormModern.cs" "%~dp0src\AssemblyInfo.cs"
if errorlevel 1 goto FAIL
echo BUILD OK: %OUT%
goto END
:FAIL
echo BUILD FAILED
:END
endlocal

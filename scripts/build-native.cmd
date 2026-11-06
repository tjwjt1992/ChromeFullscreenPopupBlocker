@echo off
setlocal
call "%~1" x64
if errorlevel 1 exit /b 1
set "ARCH=x64"
set "ROOT=%~dp0.."
set "OUT=%ROOT%\build\%ARCH%"
set "MH=%ROOT%\third_party\minhook"
if not exist "%OUT%\obj" mkdir "%OUT%\obj"
cd /d "%OUT%\obj"
cl /nologo /O2 /MT /W4 /LD /I"%MH%\include" "%ROOT%\src\Hook\PopupHook.cpp" "%MH%\src\buffer.c" "%MH%\src\hook.c" "%MH%\src\trampoline.c" "%MH%\src\hde\hde64.c" /link user32.lib /OUT:"%OUT%\PopupHook-%ARCH%.dll" /IMPLIB:"PopupHook-%ARCH%.lib"
if errorlevel 1 exit /b 1
set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
"%CSC%" /nologo /codepage:65001 /target:winexe /platform:%ARCH% /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /out:"%OUT%\FullscreenPopupBlocker-%ARCH%.exe" "%ROOT%\src\Launcher\*.cs"
exit /b %errorlevel%


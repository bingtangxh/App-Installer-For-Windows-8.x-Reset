@echo off
if exist "%SystemRoot%\SysWOW64" path %path%;%windir%\SysNative;%SystemRoot%\SysWOW64;%~dp0
bcdedit >nul
if '%errorlevel%' NEQ '0' (goto UACPrompt) else (goto UACAdmin)
:UACPrompt
%1 start "" mshta vbscript:createobject("shell.application").shellexecute("""%~0""","::",,"runas",1)(window.close)&exit
exit /B
:UACAdmin
cd /d "%~dp0"
for /r %%i in (*.appx) do powershell add-appxpackage "%%i"
for /r %%i in (*.appxbundle) do powershell add-appxpackage "%%i"
for /r %%i in (*.msix) do powershell add-appxpackage "%%i"
for /r %%i in (*.msixbundle) do powershell add-appxpackage "%%i"
for /r %%i in (*.appinstaller) do powershell add-appxpackage "%%i"
exit /b
@ECHO OFF

setlocal
set EnableNuGetPackageRestore=true

set MSBUILDEXE=msbuild.exe

set cfgOption=/p:Configuration=Release
REM set cfgOption=/p:Configuration=Debug
REM set cfgOption=/p:Configuration=Debug;Release
if not "%1"=="" set cfgOption=/p:Configuration=

set logOptions=/v:n /flp:Summary;Verbosity=diag;LogFile=msbuild.log /flp1:warningsonly;logfile=msbuild.wrn /flp2:errorsonly;logfile=msbuild.err
REM set logOptions=/v:diag /flp:Summary;Verbosity=normal;LogFile=msbuild.log /flp1:warningsonly;logfile=msbuild.wrn /flp2:errorsonly;logfile=msbuild.err

echo Please build from VS 2015(or newer version) Developer Command Prompt

%MSBUILDEXE% "%~dp0\RoslynCodeProvider.msbuild" /t:BuildAll %logOptions% /maxcpucount /nodeReuse:false %cfgOption%%*
if %ERRORLEVEL% neq 0 goto BuildFail
goto BuildSuccess

:BuildFail
echo.
echo *** BUILD FAILED ***
exit /B 999

:BuildSuccess
echo.
echo **** BUILD SUCCESSFUL ***
exit /B 0

@ECHO OFF

setlocal

set EnableNuGetPackageRestore=true

set logOptions=/flp:Summary;Verbosity=normal;LogFile=msbuild.log /flp1:warningsonly;logfile=msbuild.wrn /flp2:errorsonly;logfile=msbuild.err

echo Please build from VS 2015(or newer version) Developer Command Prompt

msbuild "%~dp0\RoslynCodeProvider.msbuild" %logOptions% /v:minimal /maxcpucount /nodeReuse:false %*
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

@ECHO OFF

setlocal

set EnableNuGetPackageRestore=true

set logOptions=/flp:Summary;Verbosity=normal;LogFile=msbuild.log /flp1:warningsonly;logfile=msbuild.wrn /flp2:errorsonly;logfile=msbuild.err

REM Find the most recent 32bit MSBuild.exe on the system. Require v12.0 (installed with VS2013) or later since .NET 4.0
REM is not supported. Always quote the %MSBuild% value when setting the variable and never quote %MSBuild% references.
set MSBuild="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %MSBuild% @set MSBuild="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %MSBuild% (
  echo Could not find msbuild.exe. Please run this from a Visual Studio developer prompt
  goto BuildFail
)

%MSBuild% "%~dp0\RoslynCodeProvider.msbuild" %logOptions% /v:minimal /maxcpucount /nodeReuse:false %*
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

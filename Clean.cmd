@ECHO OFF

setlocal

set MSBUILDEXE=msbuild.exe

set cfgOption=/p:Configuration=Release
REM set cfgOption=/p:Configuration=Debug
REM set cfgOption=/p:Configuration=Debug;Release
if not "%1"=="" set cfgOption=/p:Configuration=

set logOptions=/v:n /flp:Summary;Verbosity=diag;LogFile=msbuild.log /flp1:warningsonly;logfile=msbuild.wrn /flp2:errorsonly;logfile=msbuild.err
REM set logOptions=/v:diag /flp:Summary;Verbosity=diag;LogFile=msbuild.log /flp1:warningsonly;logfile=msbuild.wrn /flp2:errorsonly;logfile=msbuild.err

%MSBUILDEXE% "%~dp0\RoslynCodeProvider.msbuild" /t:Clean %logOptions% /maxcpucount /nodeReuse:false %cfgOption%%*
pushd %~dp0\src\Packages\Microsoft.CodeDom.Providers.DotNetCompilerPlatform
rd /q /s tools
rd /q /s tasks
rd /q /s build.pp
popd
rd /q /s bin
rd /q /s obj
del /F msbuild.*

endlocal
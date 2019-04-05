@echo off

:: Select an appropriate VsDevCmd.bat. The ordering is important. 
:: CDPx build machines will look for 2017\Enterprise
:: A local dev box might have 2017 preview or 2019 preview
set "VSROOTDIR=%ProgramFiles(x86)%\Microsoft Visual Studio"
set "VSDEVCMD="
if not exist "%VSDEVCMD%" set "VSDEVCMD=%VSROOTDIR%\2019\Enterprise\Common7\Tools\VsDevCmd.bat"
if not exist "%VSDEVCMD%" set "VSDEVCMD=%VSROOTDIR%\2019\Preview\Common7\Tools\VsDevCmd.bat"
if not exist "%VSDEVCMD%" set "VSDEVCMD=%VSROOTDIR%\2017\Enterprise\Common7\Tools\VsDevCmd.bat"
if not exist "%VSDEVCMD%" set "VSDEVCMD=%VSROOTDIR%\Preview\Enterprise\Common7\Tools\VsDevCmd.bat"
if not exist "%VSDEVCMD%" (
    echo Could not locate VsDevCmd.bat under "%VSROOTDIR%""
    set VSROOTDIR=
    set VSDEVCMD=
    exit /b 1
)

set "VSDEVCMD_ARGS=%*"
if not defined VSDEVCMD_ARGS (
    set "VSDEVCMD_ARGS=-arch=amd64 -host_arch=amd64 /no_logo"
)

:: Initialize the developer environment just like a developer box. 
:: Note that 'call' keyword that ensures that the script does not exist after 
:: calling the other batch file.
call "%VSDEVCMD%" %VSDEVCMD_ARGS%

:: Run self-check on the environment that was initialized by the above.
:: call "%VSDEVCMD%" %VSDEVCMD_ARGS% -test

set VSROOTDIR=
set VSDEVCMD=
set VSDEVCMD_ARGS=
exit /b 0

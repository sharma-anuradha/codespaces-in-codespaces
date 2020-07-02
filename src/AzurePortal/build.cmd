call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64

pushd "%~dp0"

call dir

echo Start to msbuild sln.
msbuild /m /p:Configuration=Release "%~dp0src\Default.sln"
set EX=%ERRORLEVEL%
if "%EX%" neq "0" (
    echo "Failed to msbuild Default.sln."
	goto EXITNOW
)

:EXITNOW

rem Restore working directory of user so this works fine in dev box.
popd

rem Exit with explicit 0 code so that build does not fail.
exit /B %EX%

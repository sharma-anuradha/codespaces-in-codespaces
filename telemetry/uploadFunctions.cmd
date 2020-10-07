@echo off
setlocal
cd /d %~dp0
dotnet run --no-build -p ..\tools\KustoCompiler\KustoCompiler\KustoCompiler.csproj -- runFunctionUpdate -i KustoQuery
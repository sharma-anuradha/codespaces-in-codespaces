@echo off
setlocal
cd /d %~dp0
dotnet run -p ..\tools\KustoCompiler\KustoCompiler\KustoCompiler.csproj -- compile -i KustoSource\ -o KustoQuery\
@if not defined _echo echo off
setlocal
set root=%~dp0
cd %root%
call :main
exit /b %errorloevel%

:main
    if exist projects.txt del projects.txt
    dir /s /b "%root%..\Resources\*.csproj"   >> projects.txt
    dir /s /b "%root%..\Common\*.csproj"    >> projects.txt
    dir /s /b "%root%..\Deploy\*.csproj"    >> projects.txt
    dir /s /b "%root%..\Codespaces\*.csproj"  >> projects.txt
    type projects.txt | sort > projects2.txt
    copy /y projects2.txt projects.txt > nul
    del projects2.txt
    for /f %%f in ('type projects.txt') do @dotnet sln add "%%f"
    rem del projects.txt
    exit /b 0

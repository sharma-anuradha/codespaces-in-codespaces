@echo off

call :write_capacity Development westus2

call :write_capacity Staging eastus
call :write_capacity Staging southeastasia
call :write_capacity Staging westus2
call :write_capacity Staging westeurope

call :write_capacity Production eastus
call :write_capacity Production southeastasia
call :write_capacity Production westus2
call :write_capacity Production westeurope

exit /b

:write_capacity
set env=%1
set location=%2
set output=capacity.%env%.%location%.json
echo Writing capacity to %output%...
call dotnet Catalog.dll subscriptions -c -e %env% -l %location% > %output%
type %output%
exit /b

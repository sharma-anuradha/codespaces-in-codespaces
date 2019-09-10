@if not defined _echo echo off

:init
setlocal
set subscription=vsclk-core-dev
set instance=vsclk-online-dev-ci
set stamp=%instance%-usw2
set stamp_db=%stamp%-db
set instance_db=%instance%-db
set cluster=%stamp%-cluster
set stamp_storage=%stamp%-sa
set stamp_storage=%stamp_storage:-=%
set db_name=cloud-environments

REM echo %instance%
REM echo %instance_db%
REM echo %stamp%
REM echo %stamp_db%
REM echo %cluster%
REM echo %stamp_storage%
REM echo %db_name%

:delete_aks_deployments
call :invoke: az aks get-credentials -g %stamp% -n %cluster% --overwrite-existing --subscription %subscription%
call :invoke kubectl delete deployment "cloudenvironments-api-backend-web-api"
call :invoke: kubectl delete deployment "cloudenvironments-api-frontend-web-api"

:delete_frontend_data
call :invoke az cosmosdb collection delete -c cloud_environments --db-name %db_name% -n %instance_db% -g %instance% --subscription %subscription%
call :invoke az cosmosdb collection delete -c environment_billing_events --db-name %db_name% -n %instance_db% -g %instance% --subscription %subscription%
call :invoke az cosmosdb collection delete -c environment_billing_accounts --db-name %db_name% -n %instance_db% -g %instance% --subscription %subscription%

:delete_backend_data
call :invoke az cosmosdb collection delete -c resources --db-name %db_name% -n %stamp_db% -g %stamp% --subscription %subscription%

:delete_backend_storage
call :invoke az storage container delete --name resource-broker-leases --account-name %stamp_storage% --auth-mode login --subscription %subscription% -o jsonc
call :invoke az storage queue delete --name resource-job-queue --account-name %stamp_storage% --auth-mode login --subscription %subscription% -o jsonc

:done
exit /b 0

:invoke
echo %*
call %*
echo.
exit /b %errorlevel%

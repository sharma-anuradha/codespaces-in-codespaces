FROM mcr.microsoft.com/dotnet/core/aspnet:2.2.3-alpine3.9
ARG source
WORKDIR /app
COPY ${source:-bin/Release/netcoreapp2.2/publish} .
ENTRYPOINT ["dotnet", "BackendWebApi.dll"]

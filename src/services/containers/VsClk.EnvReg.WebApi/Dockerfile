# dockerfile for VsClk.EnvReg.WebApi
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2.3-alpine3.9
LABEL maintainer="johnri@microsoft.com"
ARG source
WORKDIR /app
COPY ${source:-publish} .
ENTRYPOINT ["dotnet", "VsClk.EnvReg.WebApi.dll"]

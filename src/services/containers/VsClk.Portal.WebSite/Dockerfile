# dockerfile for VsClk.Portal.WebSite
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.2-alpine3.10
LABEL maintainer="johnri@microsoft.com"
ARG source
WORKDIR /app
COPY ${source:-.} .
ENTRYPOINT ["dotnet", "Microsoft.VsCloudKernel.Services.VsClk.Portal.WebSite.dll"]

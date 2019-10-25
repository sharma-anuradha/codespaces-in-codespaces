# dockerfile for VsClk.Portal.WebSite
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2.7-alpine3.9
LABEL maintainer="johnri@microsoft.com"
ARG source
WORKDIR /app
COPY ${source:-publish} .
ENTRYPOINT ["dotnet", "Microsoft.VsCloudKernel.Services.VsClk.Portal.WebSite.dll"]

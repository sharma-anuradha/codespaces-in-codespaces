# dockerfile for VsClk.BackplaneService
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2.7-alpine3.9
LABEL maintainer="rodrigov@microsoft.com"
ARG source
WORKDIR /app
COPY ${source:-publish} .
ENTRYPOINT ["dotnet", "Microsoft.VsCloudKernel.BackplaneService.dll"]

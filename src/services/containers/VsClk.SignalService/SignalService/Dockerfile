# dockerfile for VsClk.SignalService
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.0-alpine3.10
LABEL maintainer="rodrigov@microsoft.com"
ARG source
WORKDIR /app
COPY ${source:-publish} .
ENTRYPOINT ["dotnet", "Microsoft.VsCloudKernel.SignalService.dll"]

# dockerfile for VsClk.SignalService
FROM microsoft/dotnet:2.1.5-aspnetcore-runtime-alpine
LABEL maintainer="rodrigov@microsoft.com"
ARG source
WORKDIR /app
COPY ${source:-publish} .
ENTRYPOINT ["dotnet", "Microsoft.VsCloudKernel.SignalService.dll"]

# dockerfile for VsClk.SignalService
FROM microsoft_dotnet_core/aspnet:2.2.3-alpine3.9
LABEL maintainer="rodrigov@microsoft.com"
ARG source
WORKDIR /app
COPY ${source:-publish} .
ENTRYPOINT ["dotnet", "Microsoft.VsCloudKernel.SignalService.dll"]

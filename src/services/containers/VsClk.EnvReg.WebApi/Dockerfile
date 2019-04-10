# dockerfile for VsClk.EnvReg.WebApi
FROM microsoft_dotnet_core/aspnet:2.2.3-alpine3.9
LABEL maintainer="johnri@microsoft.com"
ARG source
WORKDIR /app
COPY ${source:-publish} .
ENTRYPOINT ["dotnet", "Microsoft.VsCloudKernel.Services.VsClk.EnvReg.WebApi.dll"]

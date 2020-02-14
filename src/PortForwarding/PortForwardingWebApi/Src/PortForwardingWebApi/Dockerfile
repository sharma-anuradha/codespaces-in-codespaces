FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.0-alpine3.10
ARG source
WORKDIR /app
COPY ${source:-publish} .
ENTRYPOINT ["dotnet", "Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.dll"]

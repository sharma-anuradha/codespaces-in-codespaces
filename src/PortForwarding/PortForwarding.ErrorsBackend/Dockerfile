FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.2-alpine3.10
ARG source

WORKDIR /app
COPY ${source:-publish} .

CMD ["dotnet", "Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.ErrorsBackend.dll"]

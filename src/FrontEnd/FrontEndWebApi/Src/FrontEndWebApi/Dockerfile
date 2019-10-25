FROM mcr.microsoft.com/dotnet/core/aspnet:2.2.7-alpine3.9
ARG source
WORKDIR /app
COPY ${source:-publish} .
ENTRYPOINT ["dotnet", "Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.dll"]

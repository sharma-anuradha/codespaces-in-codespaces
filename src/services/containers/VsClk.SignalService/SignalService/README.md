# BUILD STEPS
- dotnet publish -c Release -o ./publish

# PUBLISH TO DOCKER REPOSITORY
- docker build . -t vsclk.signalservice:0.1
- docker tag vsclk.signalservice:0.1 vslsdev.azurecr.io/vsclk.signalservice:v1
- az account set --subscription vsengsaas-liveshare-dev
- az acr login --name vslsdev
- docker push vslsdev.azurecr.io/vsclk.signalservice:v1

# RUNNING LOCALLY
docker run -p 8080:80 -it --rm vsclk.signalservice:0.1

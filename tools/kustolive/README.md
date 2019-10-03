
# Usage

```
az acr login -n vsclkapps
docker pull vsclkapps.azurecr.io/kustolive:latest
dotnet BackEndWebApi.dll | docker run -i -e USER=$USER -v ~/CEDev/appsettings.json:/root/CEDev/appsettings.json vsclkapps.azurecr.io/kustolive:latest
```

# Building docker image

```
docker build .
docker tag IMAGEID vsclkapps.azurecr.io/kustolive:0.1
docker tag IMAGEID vsclkapps.azurecr.io/kustolive:latest
az acr login -n vsclkapps
docker push vsclkapps.azurecr.io/kustolive:0.1
docker push vsclkapps.azurecr.io/kustolive:latest
```

# Development

```
python3 -m venv env
source env/bin/activate
pip install -r requirements.txt
```


## NOTES:
If a new database needs to be configured, create a streaming ingestion policy through Azure Data Explorer web UI first.

For example:
```
.alter database VsoDevStampEventLogs policy streamingingestion '{  "NumberOfRowStores": 4}'
```
More info at https://docs.microsoft.com/en-us/azure/data-explorer/ingest-data-streaming
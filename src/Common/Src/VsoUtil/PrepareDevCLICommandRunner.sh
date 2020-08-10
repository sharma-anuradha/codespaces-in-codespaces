#!/bin/bash

# Initialize user appsettings
if [ -z "$CEDEV_APPSETTINGS" ]
then
	echo "CEDEV_APPSETTINGS is not set"
	exit 11
fi

printf %s $CEDEV_APPSETTINGS > ~/CEDev/appsettings.json

# Azure login
echo "Loging into Azure CLI..."
az login

# Start ngrok tunnel
echo "Starting ngrok tunnel for Frontend"
pkill ngrok
sleep 2
nohup ngrok http 53760&

# Waiting for the tunnel
attempt=0
while [ $attempt -lt 10 ]
do
	echo "Waiting for ngrok $attempt ..."

	pid=`pgrep ngrok`
	if [ ! -z "$pid" ]
	then
		ngHost=`curl --silent --show-error http://127.0.0.1:4040/api/tunnels | sed -nE 's/.*public_url":"https:..([^"]*).*/\1/p'`
		if [ ! -z "$ngHost" ]
		then
			echo Ngrok is running - $ngHost
			break
		else
			echo Ngrok is running but no tunnels found - Waiting...
		fi
	fi

	attempt=`expr $attempt + 1`
	sleep 3
done

if [ -z "$ngHost" ]
	then
		echo Error: No ngrok tunnels established.
		exit 12
fi

# Run VsoUtil prepare dev cli
echo "Running VsoUtil preparedevcli -f $ngHost"
UseSecretFromAppConfig=1
dotnet VsoUtil.dll preparedevcli -f $ngHost

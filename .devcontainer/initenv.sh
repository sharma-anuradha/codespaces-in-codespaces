#!/bin/sh

# Install .NET Core 3.1.100 SDK. Need to use the user-local install, not the package manager install.
# TODO: Remove once 3.1.100 is included in the Oryx base image
wget -q https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 3.1.100
rm dotnet-install.sh

# Install Azure DevOps credential helper for dotnet restore.
wget -q https://raw.githubusercontent.com/microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh -O installcredprovider.sh
chmod +x installcredprovider.sh
./installcredprovider.sh
rm installcredprovider.sh

#!/bin/sh

# Register Microsoft key and feed for .NET Core
wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install the .NET Core SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-2.2

# Install Azure DevOps credential helper for dotnet restore.
wget -q https://raw.githubusercontent.com/microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh -O installcredprovider.sh
chmod +x installcredprovider.sh
./installcredprovider.sh
rm installcredprovider.sh
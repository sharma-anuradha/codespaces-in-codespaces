#!/bin/sh

# Set env var with a base64 encoded DEVDIV_PAT for npmrc to consume
echo export DEVDIV_PAT_BASE64="\$(printf "%s" \"\${DEVDIV_PAT}\" | base64)" >> ~/.bashrc

# Interpolate credential env vars inside nuget_feed_auth.json and set it to VSS_NUGET_EXTERNAL_FEED_ENDPOINTS, for credential manager to consume
# dotnet restore command will use this credentials via credential manager (https://github.com/microsoft/artifacts-credprovider#environment-variables)
cp .devcontainer/nuget_feed_auth.json ~/.nuget.json
echo export VSS_NUGET_EXTERNAL_FEED_ENDPOINTS=\`eval \"printf %s \''$(cat ~/.nuget.json)'\'\"\` >> ~/.bashrc

source ~/.bashrc

# Create user .npmrc file
cp .devcontainer/user_npmrc ~/.npmrc

# Install .NET Core 3.1.200 SDK. Need to use the user-local install, not the package manager install.
# TODO: Remove once 3.1.200 is included in the Oryx base image
wget -q https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 3.1.200
rm dotnet-install.sh

# Install Azure DevOps credential helper for dotnet restore.
wget -q https://raw.githubusercontent.com/microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh -O installcredprovider.sh
chmod +x installcredprovider.sh
./installcredprovider.sh
rm installcredprovider.sh

# Install terraform
wget -q https://releases.hashicorp.com/terraform/0.12.26/terraform_0.12.26_linux_amd64.zip -O terraform.zip
sudo unzip -o terraform -d /usr/local/bin/
sudo chmod +x /usr/local/bin/terraform
rm terraform.zip

pushd build/port-forwarding
terraform init
popd
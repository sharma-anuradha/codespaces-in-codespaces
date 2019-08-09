#!/bin/sh
echo "Updating packages ..."
apt update
apt upgrade -y
echo "Install docker ..."
sudo apt-get install -y docker.io
echo "Install ORAS ..."
curl -LO https://github.com/deislabs/oras/releases/download/v0.5.0/oras_0.5.0_linux_amd64.tar.gz
# unpack, install, dispose
mkdir -p oras-install/
tar -zxf oras_0.5.0_*.tar.gz -C oras-install/
mv oras-install/oras /usr/local/bin/
rm -rf oras_0.5.0_*.tar.gz oras-install/
echo "Pull VM Agent..."
mkdir /vmagent
cd /vmagent
#oras pull -u <userid> -p <password> <acrUri/image:tag>
#apt-get install unzip
#unzip CLI_linux*.zip
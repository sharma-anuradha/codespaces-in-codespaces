#!/bin/bash -e

# IP_ADDR: IP address to have the DNS server resolve to instead of 127.0.0.1
# greps for the current IP of the WSL2 VM
IP_ADDR=$(ip addr | grep -oP "inet \\K(?!127.0.0.1)[^/]+")

# CONFIGS_DIR: Where to save the updated DNS configurations files
# relative to this path:
cd $(dirname $0)/../../../portal-local-dns
export CONFIGS_DIR=bld

mkdir -p $CONFIGS_DIR
cp Corefile *.* $CONFIGS_DIR
cd $CONFIGS_DIR
sed -i "s/127.0.0.1/$IP_ADDR/" *.*

cd ..
docker-compose --env-file ../dev-local.env up -d --build

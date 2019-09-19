#!/bin/sh
echo "Set script to fail if command in script returns non zero return code"
set -eu pipefall

SCRIPT_PARAM_VMTOKEN=''
echo $SCRIPT_PARAM_VMTOKEN > /.vmtoken

echo "Updating packages ..."
apt update || true && apt upgrade -y 

echo "Increase file watcher limit"
echo "fs.inotify.max_user_watches=524288" | tee -a /etc/sysctl.conf
sysctl -p

echo "Install docker ..."
apt-get install -y docker.io
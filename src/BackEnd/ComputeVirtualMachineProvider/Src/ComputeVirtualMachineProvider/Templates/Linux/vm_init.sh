#!/bin/sh
echo "Set script to fail if command in script returns non zero return code"
set -eu pipefall

SCRIPT_PARAM_VMTOKEN=''
SCRIPT_PARAM_VM_QUEUE_TOKEN=''
SCRIPT_PARAM_VMAGENT_BLOB_URL=''
echo $SCRIPT_PARAM_VMTOKEN > /.vmtoken
echo $SCRIPT_PARAM_VM_QUEUE_TOKEN > /.queuetoken

echo "Updating packages ..."
apt update || true && apt upgrade -y 

echo "Increase file watcher limit"
echo "fs.inotify.max_user_watches=524288" | tee -a /etc/sysctl.conf
sysctl -p

echo "Install docker ..."
apt-get install -y docker.io

echo "download vso agent"
mkdir -p /.cloudenv/vsoagent
cd /.cloudenv/vsoagent/
curl -o vsoagent.zip $SCRIPT_PARAM_VMAGENT_BLOB_URL
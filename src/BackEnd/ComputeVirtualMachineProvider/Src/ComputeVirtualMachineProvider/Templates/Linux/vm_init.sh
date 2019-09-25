#!/bin/bash
echo "Set script to fail if command in script returns non zero return code"
set -eu pipefall

SCRIPT_PARAM_VMAGENT_BLOB_URL='__REPLACE_VMAGENT_BLOB_URl__'
SCRIPT_PARAM_VMTOKEN='__REPLACE_VMTOKEN__'
SCRIPT_PARAM_VM_QUEUE_TOKEN='__REPLACE_VM_QUEUE_TOKEN__'
SCRIPT_PARAM_RESOURCEID='__REPLACE_RESOURCEID__'
SCRIPT_PARAM_FRONTEND_SERVICE_BASEURI='__REPLACE_FRONTEND_SERVICE_DNS_HOST_NAME__'

echo "Updating packages ..."
apt update || true

echo "Increase file watcher limit"
echo "fs.inotify.max_user_watches=524288" | tee -a /etc/sysctl.conf
sysctl -p

echo "Install docker ..."
apt-get install -y docker.io

echo "Install unzip ..."
apt-get -yq update && apt-get install -y --no-install-recommends unzip

echo "Download vso agent ..."
mkdir -p /.vsonline/vsoagent/bin/appdata
cd /.vsonline/vsoagent/bin
wget -qO- -O tmp.zip $SCRIPT_PARAM_VMAGENT_BLOB_URL && unzip tmp.zip && rm tmp.zip

echo "Install vso agent ..."
chmod +x install_vmagent.sh uninstall_vmagent.sh
./install_vmagent.sh

echo "Add environment variarible ..."
echo export VSOAGENT_ENVAGENTSETTINGS__INPUT_QUEUE_TOKEN=$SCRIPT_PARAM_VM_QUEUE_TOKEN >> /etc/environment
echo export VSOAGENT_HEARTBEATSETTINGS__VMTOKEN=$SCRIPT_PARAM_VMTOKEN >> /etc/environment
echo export VSOAGENT_HEARTBEATSETTINGS__RESOURCEID=$SCRIPT_PARAM_RESOURCEID >> /etc/environment
echo export VSOAGENT_HEARTBEATSETTINGS__SERVICEBASEURI=$SCRIPT_PARAM_FRONTEND_SERVICE_BASEURI >> /etc/environment
source /etc/environment

echo "Start vso agent"
systemctl start vmagent.service

echo "Check service status"
systemctl status vmagent.service

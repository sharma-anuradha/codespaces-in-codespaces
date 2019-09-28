#!/bin/bash
echo "Set script to fail if command in script returns non zero return code"
set -eu pipefall

SCRIPT_PARAM_VMAGENT_BLOB_URL='__REPLACE_VMAGENT_BLOB_URl__'
SCRIPT_PARAM_VMTOKEN='__REPLACE_VMTOKEN__'
SCRIPT_PARAM_RESOURCEID='__REPLACE_RESOURCEID__'
SCRIPT_PARAM_FRONTEND_DNSHOSTNAME='__REPLACE_FRONTEND_SERVICE_DNS_HOST_NAME__'
SCRIPT_PARAM_VM_QUEUE_NAME='__REPLACE_INPUT_QUEUE_NAME__'
SCRIPT_PARAM_VM_QUEUE_URL='__REPLACE_INPUT_QUEUE_URL__'
SCRIPT_PARAM_VM_QUEUE_SASTOKEN='__REPLACE_INPUT_QUEUE_SASTOKEN__'

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

echo "Create configuration file ..."
echo "[ENVAGENTSETTINGS]">> /.vsonline/vsoagent/bin/config.ini
echo "INPUTQUEUENAME=$SCRIPT_PARAM_VM_QUEUE_NAME" >> /.vsonline/vsoagent/bin/config.ini
echo "INPUTQUEUEURL=$SCRIPT_PARAM_VM_QUEUE_URL" >> /.vsonline/vsoagent/bin/config.ini
echo "INPUTQUEUESASTOKEN=$SCRIPT_PARAM_VM_QUEUE_SASTOKEN" >> /.vsonline/vsoagent/bin/config.ini
echo "[HEARTBEATSETTINGS]">> /.vsonline/vsoagent/bin/config.ini
echo "VMTOKEN=$SCRIPT_PARAM_VMTOKEN" >> /.vsonline/vsoagent/bin/config.ini
echo "RESOURCEID=$SCRIPT_PARAM_RESOURCEID" >> /.vsonline/vsoagent/bin/config.ini
echo "SERVICEHOSTNAME=$SCRIPT_PARAM_FRONTEND_DNSHOSTNAME" >> /.vsonline/vsoagent/bin/config.ini

echo "Start vso agent"
systemctl start vmagent.service

echo "Check service status"
systemctl status vmagent.service

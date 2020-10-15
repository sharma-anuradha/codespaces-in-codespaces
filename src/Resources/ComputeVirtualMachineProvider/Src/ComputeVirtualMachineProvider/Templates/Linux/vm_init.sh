#!/bin/bash

# External dependecies are pre-baked to the custom VM image by .pipelines/vm_setup.sh

echo "Set script to fail if command in script returns non zero return code"
set -exu pipefall

SCRIPT_PARAM_VMAGENT_BLOB_URL='__REPLACE_VMAGENT_BLOB_URl__'
SCRIPT_PARAM_VMTOKEN='__REPLACE_VMTOKEN__'
SCRIPT_PARAM_RESOURCEID='__REPLACE_RESOURCEID__'
SCRIPT_PARAM_FRONTEND_DNSHOSTNAME='__REPLACE_FRONTEND_SERVICE_DNS_HOST_NAME__'
SCRIPT_PARAM_VM_QUEUE_NAME='__REPLACE_INPUT_QUEUE_NAME__'
SCRIPT_PARAM_VM_QUEUE_URL='__REPLACE_INPUT_QUEUE_URL__'
SCRIPT_PARAM_VM_QUEUE_SASTOKEN='__REPLACE_INPUT_QUEUE_SASTOKEN__'
SCRIPT_PARAM_VM_USE_OUTPUT_QUEUE=0
SCRIPT_PARAM_VM_OUTPUT_QUEUE_NAME='__REPLACE_OUTPUT_QUEUE_NAME__'
SCRIPT_PARAM_VM_OUTPUT_QUEUE_URL='__REPLACE_OUTPUT_QUEUE_URL__'
SCRIPT_PARAM_VM_OUTPUT_QUEUE_SASTOKEN='__REPLACE_OUTPUT_QUEUE_SASTOKEN__'
SCRIPT_PARAM_VM_PUBLIC_KEY_PATH='__REPLACE_VM_PUBLIC_KEY_PATH__'

# wait for cloud-init to finish before proceeding.
cloud-init status --wait

echo "Verify docker ..."
docker --version
docker-compose --version

echo "Create container temp folder"
containerTmp=/mnt/containerTmp
mkdir -p $containerTmp
chmod o+rwt $containerTmp
setfacl -dR -m o::rw $containerTmp

echo "Download codespaces agent ..."
mkdir -p /.codespaces/agent/bin
cd /.codespaces/agent/bin
wget -qO- -O tmp.zip $SCRIPT_PARAM_VMAGENT_BLOB_URL && unzip tmp.zip && rm tmp.zip

echo "Create codespaces shared folder with appropriate permissions"
codespacesSharedFolder=~/.codespaces/shared
mkdir -p $codespacesSharedFolder
chmod o+rw $codespacesSharedFolder
setfacl -dR -m o::rw $codespacesSharedFolder

# BACKWARDS COMPATIBILITY
# Needed for old containers that reference the old folder.
echo "Create vso shared folder with appropriate permissions"
vsoSharedFolder=~/.vsonline/.vsoshared
mkdir -p $vsoSharedFolder
chmod o+rw $vsoSharedFolder
setfacl -dR -m o::rw $vsoSharedFolder

echo "Install codespaces agent ..."
chmod +x install_codespaces_agent.sh uninstall_codespaces_agent.sh
./install_codespaces_agent.sh

echo "Make copy of codespaces agent..."
mkdir -p /.codespaces/agent/mount
cp -a /.codespaces/agent/bin/. /.codespaces/agent/mount

# BACKWARDS COMPATIBILITY
# Needed for old containers that reference the old folder.
echo "Make copy of vso agent..."
mkdir -p /.vsonline/vsoagent/mount
cp -a /.codespaces/agent/bin/. /.vsonline/vsoagent/mount

codespacesAgentIni=/.codespaces/agent/bin/config.ini

echo "Create configuration file ..."
echo "[ENVAGENTSETTINGS]">> $codespacesAgentIni
echo "INPUTQUEUENAME=$SCRIPT_PARAM_VM_QUEUE_NAME" >> $codespacesAgentIni
echo "INPUTQUEUEURL=$SCRIPT_PARAM_VM_QUEUE_URL" >> $codespacesAgentIni
echo "INPUTQUEUESASTOKEN=$SCRIPT_PARAM_VM_QUEUE_SASTOKEN" >> $codespacesAgentIni
if [ $SCRIPT_PARAM_VM_USE_OUTPUT_QUEUE -eq 1 ]
 then
      echo "OUTPUTQUEUENAME=$SCRIPT_PARAM_VM_OUTPUT_QUEUE_NAME" >> $codespacesAgentIni
      echo "OUTPUTQUEUEURL=$SCRIPT_PARAM_VM_OUTPUT_QUEUE_URL" >> $codespacesAgentIni
      echo "OUTPUTQUEUESASTOKEN=$SCRIPT_PARAM_VM_OUTPUT_QUEUE_SASTOKEN" >> $codespacesAgentIni
fi
echo "[HEARTBEATSETTINGS]">> $codespacesAgentIni
echo "VMTOKEN=$SCRIPT_PARAM_VMTOKEN" >> $codespacesAgentIni
echo "RESOURCEID=$SCRIPT_PARAM_RESOURCEID" >> $codespacesAgentIni
echo "SERVICEHOSTNAME=$SCRIPT_PARAM_FRONTEND_DNSHOSTNAME" >> $codespacesAgentIni

echo 'Disabling Transparent Huge Pages'
echo 'never' > /sys/kernel/mm/transparent_hugepage/enabled
echo 'never' > /sys/kernel/mm/transparent_hugepage/defrag

echo "Start codespaces agent"
# Switch to daemon process until service permission issue is fixed.
bash -c 'nohup ./codespaces vmagent &>/dev/null & jobs -p %1'

echo "delete ssh public key"
rm -f $SCRIPT_PARAM_VM_PUBLIC_KEY_PATH

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
SCRIPT_PARAM_VM_USE_OUTPUT_QUEUE=0
SCRIPT_PARAM_VM_OUTPUT_QUEUE_NAME='__REPLACE_OUTPUT_QUEUE_NAME__'
SCRIPT_PARAM_VM_OUTPUT_QUEUE_URL='__REPLACE_OUTPUT_QUEUE_URL__'
SCRIPT_PARAM_VM_OUTPUT_QUEUE_SASTOKEN='__REPLACE_OUTPUT_QUEUE_SASTOKEN__'

echo "Updating packages ..."
apt-get -yq update || true

echo "Increase file watcher limit"
echo "fs.inotify.max_user_watches=524288" | tee -a /etc/sysctl.conf
sysctl -p

echo "Install docker ..."
# Download specific docker version - 18.09.7-0ubuntu1~18.04.4
#   URL below was taken from running 'apt-cache show docker.io=18.09.7-0ubuntu1~18.04.4'
#   OR visiting http://azure.archive.ubuntu.com/ubuntu/dists/bionic-updates/universe/binary-amd64/Packages.gz
docker_debfile=$(mktemp)
wget -qO- -O $docker_debfile http://azure.archive.ubuntu.com/ubuntu/pool/universe/d/docker.io/docker.io_18.09.7-0ubuntu1~18.04.4_amd64.deb
dpkg --install $docker_debfile || true
apt-get install -fy
rm $docker_debfile
docker --version

# Block Azure Instance Metadata Service IP on host (OUTPUT) and also in containers (DOCKER-USER)
# This needs to happen after the docker install for DOCKER-USER to exist in iptables.

echo "Block Azure Instance Metadata Service ..."
# Temporarily disable iptables-persistent which restores iptables on VM restart
#echo iptables-persistent iptables-persistent/autosave_v4 boolean true | debconf-set-selections
#echo iptables-persistent iptables-persistent/autosave_v6 boolean true | debconf-set-selections
#apt-get -yq update && apt-get install -y iptables-persistent
INSTANCE_METADATA_IP=169.254.169.254
iptables -I OUTPUT -d $INSTANCE_METADATA_IP -j DROP
iptables -I DOCKER-USER -d $INSTANCE_METADATA_IP -j DROP
# iptables-save > /etc/iptables/rules.v4

echo "Install unzip ..."
apt-get -yq update && apt-get install -y --no-install-recommends unzip

echo "Download vso agent ..."
mkdir -p /.vsonline/vsoagent/bin
cd /.vsonline/vsoagent/bin
wget -qO- -O tmp.zip $SCRIPT_PARAM_VMAGENT_BLOB_URL && unzip tmp.zip && rm tmp.zip

echo "Create vso shared folder with appropriate permissions"
vsoSharedFolder=~/.vsonline/.vsoshared
mkdir -p $vsoSharedFolder
chmod o+rw $vsoSharedFolder
setfacl -dR -m o::rw $vsoSharedFolder

echo "Install vso agent ..."
chmod +x install_vsoagent.sh uninstall_vsoagent.sh
./install_vsoagent.sh

echo "Make copy of vso agent..."
mkdir -p /.vsonline/vsoagent/mount
cp -a /.vsonline/vsoagent/bin/. /.vsonline/vsoagent/mount

echo "Install azcopy ..."
wget -q -O azcopy.tar.gz https://azcopyvnext.azureedge.net/release20200124/azcopy_linux_amd64_10.3.4.tar.gz \
    && tar -xf azcopy.tar.gz \
    && mv azcopy_linux_amd64_*/azcopy /.vsonline/azcopy

echo "Create configuration file ..."
echo "[ENVAGENTSETTINGS]">> /.vsonline/vsoagent/bin/config.ini
echo "INPUTQUEUENAME=$SCRIPT_PARAM_VM_QUEUE_NAME" >> /.vsonline/vsoagent/bin/config.ini
echo "INPUTQUEUEURL=$SCRIPT_PARAM_VM_QUEUE_URL" >> /.vsonline/vsoagent/bin/config.ini
echo "INPUTQUEUESASTOKEN=$SCRIPT_PARAM_VM_QUEUE_SASTOKEN" >> /.vsonline/vsoagent/bin/config.ini
if [ $SCRIPT_PARAM_VM_USE_OUTPUT_QUEUE -eq 1 ]
 then
      echo "OUTPUTQUEUENAME=$SCRIPT_PARAM_VM_OUTPUT_QUEUE_NAME" >> /.vsonline/vsoagent/bin/config.ini
      echo "OUTPUTQUEUEURL=$SCRIPT_PARAM_VM_OUTPUT_QUEUE_URL" >> /.vsonline/vsoagent/bin/config.ini
      echo "OUTPUTQUEUESASTOKEN=$SCRIPT_PARAM_VM_OUTPUT_QUEUE_SASTOKEN" >> /.vsonline/vsoagent/bin/config.ini
fi
echo "[HEARTBEATSETTINGS]">> /.vsonline/vsoagent/bin/config.ini
echo "VMTOKEN=$SCRIPT_PARAM_VMTOKEN" >> /.vsonline/vsoagent/bin/config.ini
echo "RESOURCEID=$SCRIPT_PARAM_RESOURCEID" >> /.vsonline/vsoagent/bin/config.ini
echo "SERVICEHOSTNAME=$SCRIPT_PARAM_FRONTEND_DNSHOSTNAME" >> /.vsonline/vsoagent/bin/config.ini

echo "Start vso agent"
# Switch to daemon process until service permission issue is fixed.
bash -c 'nohup ./vso vmagent &>/dev/null & jobs -p %1'

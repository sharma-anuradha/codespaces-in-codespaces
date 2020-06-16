#!/bin/bash
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

echo "Updating packages ..."
apt-get -yq update

echo "Increase file watcher limit"
echo "fs.inotify.max_user_watches=524288" | tee -a /etc/sysctl.conf
sysctl -p

echo "Create docker group with ID 800"
groupadd -g 800 docker

echo "Install docker ..."
# Install moby-engine=3.0.11+azure-2 (as well as dependencies: moby-cli, pigz)
# URL below was taken from https://packages.microsoft.com/ubuntu/18.04/prod/dists/bionic/main/binary-amd64/Packages
tmp_moby_engine_debfile=$(mktemp)
tmp_moby_cli_debfile=$(mktemp)
tmp_pigz_debfile=$(mktemp)
wget -qO- -O $tmp_moby_engine_debfile https://packages.microsoft.com/ubuntu/18.04/prod/pool/main/m/moby-engine/moby-engine_3.0.11+azure-2_amd64.deb
wget -qO- -O $tmp_moby_cli_debfile https://packages.microsoft.com/ubuntu/18.04/prod/pool/main/m/moby-cli/moby-cli_3.0.11+azure-2_amd64.deb
wget -qO- -O $tmp_pigz_debfile http://azure.archive.ubuntu.com/ubuntu/pool/universe/p/pigz/pigz_2.4-1_amd64.deb
dpkg --install $tmp_moby_engine_debfile $tmp_moby_cli_debfile $tmp_pigz_debfile
rm $tmp_moby_engine_debfile $tmp_moby_cli_debfile $tmp_pigz_debfile
apt-get install -fy
docker --version

echo "Install docker-compose"
curl -L "https://github.com/docker/compose/releases/download/1.25.4/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose
docker-compose --version

echo "Create container temp folder"
containerTmp=/mnt/containerTmp
mkdir -p $containerTmp
chmod o+rwt $containerTmp
setfacl -dR -m o::rw $containerTmp

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

echo "Update Time Sync Configuration ..."
# disable NTP-based time sync
timedatectl set-ntp off
# configure VMICTimeSync (host-only) time sync
apt-get -yq install chrony
cp /etc/chrony/chrony.conf /etc/chrony/chrony.conf.backup
cat > /etc/chrony/chrony.conf <<EOF
keyfile /etc/chrony/chrony.keys
driftfile /var/lib/chrony/chrony.drift
logdir /var/log/chrony
maxupdateskew 100.0
rtcsync
refclock PHC /dev/ptp0 poll 3 dpoll -2 offset 0
makestep 1.0 -1
EOF
systemctl restart chrony.service

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

echo "delete ssh public key"
rm -f $SCRIPT_PARAM_VM_PUBLIC_KEY_PATH

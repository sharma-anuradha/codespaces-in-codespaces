#!/bin/bash
echo "Set script to fail if command in script returns non zero return code"
set -exu pipefall

# wait for cloud-init to finish before proceeding.
cloud-init status --wait

echo "Add packages.microsoft.com repository"
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
apt-add-repository https://packages.microsoft.com/ubuntu/18.04/prod

echo "Increase file watcher limit"
echo "fs.inotify.max_user_watches=524288" | tee -a /etc/sysctl.conf
sysctl -p

echo "Create docker group with ID 800"
groupadd -g 800 docker

echo "Updating apt packages ..."
apt -yq update

echo "Installing moby-engine ..."
apt -yq install moby-engine

echo "Verify docker ..."
docker --version

echo "Installing unzip ..."
apt install unzip

echo "Verify unzip ..."
which unzip

echo "Installing chrony ..."
apt -yq install chrony

echo "Verify chrony ..."
which chronyd

echo "Install docker-compose"
curl -L "https://github.com/docker/compose/releases/download/1.26.2/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose

echo "Verify docker compose ..."
docker-compose --version

echo "Install azcopy ..."
mkdir /.vsonline
wget -q -O azcopy.tar.gz https://azcopyvnext.azureedge.net/release20200124/azcopy_linux_amd64_10.3.4.tar.gz \
    && tar -xf azcopy.tar.gz \
    && mv azcopy_linux_amd64_*/azcopy /.vsonline/azcopy

echo "Update Time Sync Configuration ..."
# disable NTP-based time sync
timedatectl set-ntp off
# configure VMICTimeSync (host-only) time sync
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

echo "Switch ssh port from default 22"
sed -i 's/#Port 22/Port 2000/g' /etc/ssh/sshd_config
service ssh restart

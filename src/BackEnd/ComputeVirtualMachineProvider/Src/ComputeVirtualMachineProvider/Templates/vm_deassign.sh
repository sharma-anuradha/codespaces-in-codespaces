#!/bin/sh
# Stop docker, unmount and detach attached loop device
systemctl stop docker
umount /var/lib/docker
rm /etc/smbcredentials/cloudenvstorage.cred
LOOP_DEVICE=`losetup | grep "/mnt/cloudenvdata/dockerlib" | cut -d" " -f1`
losetup -d $LOOP_DEVICE

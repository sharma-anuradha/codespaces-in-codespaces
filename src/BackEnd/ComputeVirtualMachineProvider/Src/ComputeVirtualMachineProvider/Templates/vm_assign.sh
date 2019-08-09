#!/bin/sh
# Mount storage and start the container

# e.g. SCRIPT_PARAM_STORAGE='{"storageAccountName":"mystorage","storageAccountKey":"passwordhere","storageShareName":"sharename","storageFileName":"filename"}'
# e.g. SCRIPT_PARAM_CONTAINER_ENV_VARS='{"SESSION_ID":"AAAA0000BBBB1111CCCC2222DDDD3333EEEE","SESSION_TOKEN":"abc123","SESSION_CALLBACK":"https://.../api/environment/registration/./_callback","GIT_REPO_URL":"https://github.com/vsls-contrib/guestbook"}'

SCRIPT_PARAM_STORAGE=''
SCRIPT_PARAM_CONTAINER_ENV_VARS=''

[ -z "$SCRIPT_PARAM_STORAGE" ] && echo "SCRIPT_PARAM_STORAGE parameter not set." && exit 1;
[ -z "$SCRIPT_PARAM_CONTAINER_ENV_VARS" ] && echo "SCRIPT_PARAM_CONTAINER_ENV_VARS parameter not set." && exit 1;

CLOUDENVSTORAGE_ACCOUNT=$(echo $SCRIPT_PARAM_STORAGE | python -c "import sys, json; print json.load(sys.stdin)['storageAccountName']")
CLOUDENVSTORAGE_PASS=$(echo $SCRIPT_PARAM_STORAGE | python -c "import sys, json; print json.load(sys.stdin)['storageAccountKey']")
CLOUDENVSTORAGE_SHARE=$(echo $SCRIPT_PARAM_STORAGE | python -c "import sys, json; print json.load(sys.stdin)['storageShareName']")
CLOUDENVSTORAGE_FILE=$(echo $SCRIPT_PARAM_STORAGE | python -c "import sys, json; print json.load(sys.stdin)['storageFileName']")

[ -z "$CLOUDENVSTORAGE_ACCOUNT" ] && echo "CLOUDENVSTORAGE_ACCOUNT parameter not set." && exit 1;
[ -z "$CLOUDENVSTORAGE_PASS" ] && echo "CLOUDENVSTORAGE_PASS parameter not set." && exit 1;
[ -z "$CLOUDENVSTORAGE_SHARE" ] && echo "CLOUDENVSTORAGE_SHARE parameter not set." && exit 1;
[ -z "$CLOUDENVSTORAGE_FILE" ] && echo "CLOUDENVSTORAGE_FILE parameter not set." && exit 1;

CONTAINER_ENVIRONMENT_VARIABLES=$(echo $SCRIPT_PARAM_CONTAINER_ENV_VARS | python -c "import sys, json; print ('\n'.join([k+'='+v for k,v in json.load(sys.stdin).items()]))")

mkdir -p /etc/smbcredentials
printf "username=$CLOUDENVSTORAGE_ACCOUNT\npassword=$CLOUDENVSTORAGE_PASS\n" > /etc/smbcredentials/cloudenvstorage.cred
chmod 600 /etc/smbcredentials/cloudenvstorage.cred

# Stop current running docker daemon
systemctl stop docker

# Actual mount of the File Share here
mkdir -p /mnt/$CLOUDENVSTORAGE_SHARE
mount -t cifs //$CLOUDENVSTORAGE_ACCOUNT.file.core.windows.net/$CLOUDENVSTORAGE_SHARE /mnt/$CLOUDENVSTORAGE_SHARE -o vers=3.0,credentials=/etc/smbcredentials/cloudenvstorage.cred,dir_mode=0777,file_mode=0777,serverino

losetup -Pf /mnt/$CLOUDENVSTORAGE_SHARE/$CLOUDENVSTORAGE_FILE
LOOP_DEVICE=`losetup | grep "/mnt/$CLOUDENVSTORAGE_SHARE/$CLOUDENVSTORAGE_FILE" | cut -d" " -f1`
mount "$LOOP_DEVICE" /var/lib/docker -o nosuid,nodev
	
# Start docker again
systemctl start docker

CONTAINER_IMAGE_ID=$(docker images "vsclkapps.azurecr.io/kitchensink" --quiet)

# Get the environment variables and save to file
CONTAINER_ENVS_FILE=$(mktemp)
echo "$CONTAINER_ENVIRONMENT_VARIABLES" > $CONTAINER_ENVS_FILE

# Start the container with the environment variables
docker run -d --restart=unless-stopped --env-file $CONTAINER_ENVS_FILE $CONTAINER_IMAGE_ID

# cleanup
rm $CONTAINER_ENVS_FILE
unset SCRIPT_PARAM_STORAGE
unset SCRIPT_PARAM_CONTAINER_ENV_VARS
unset CLOUDENVSTORAGE_ACCOUNT
unset CLOUDENVSTORAGE_PASS
unset CLOUDENVSTORAGE_SHARE
unset CLOUDENVSTORAGE_FILE
unset CONTAINER_ENVIRONMENT_VARIABLES
unset LOOP_DEVICE
unset CONTAINER_IMAGE_ID
unset CONTAINER_ENVS_FILE

echo "Done."

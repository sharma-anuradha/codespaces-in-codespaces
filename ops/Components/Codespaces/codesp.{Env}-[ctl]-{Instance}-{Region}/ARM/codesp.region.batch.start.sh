# script parameters
SCRIPT_PARAM_GCS_ENVIRONMENT='{{{azSecPackGcsEnvironment}}}'
SCRIPT_PARAM_GCS_ACCOUNT='{{{azSecPackGcsAccount}}}'
SCRIPT_PARAM_GCS_NAMESPACE='{{{azSecPackNamespace}}}'
SCRIPT_PARAM_GCS_ROLE='{{{azSecPackRole}}}'
SCRIPT_PARAM_GCS_TENANT='{{{azSecPackTenant}}}'
SCRIPT_PARAM_GCS_PFX_BASE64='$1'

# Install azcopy
wget --no-verbose -O azcopy.tar.gz https://azcopyvnext.azureedge.net/release20200124/azcopy_linux_amd64_10.3.4.tar.gz \
    && tar -xf azcopy.tar.gz \
    && mv azcopy_linux_amd64_*/azcopy $AZ_BATCH_NODE_SHARED_DIR/azcopy \
    && chmod +x $AZ_BATCH_NODE_SHARED_DIR/azcopy

# Set up data disk
sfdisk -l
DISK_NAME=`sfdisk -l | grep '2 TiB' -m 1| cut -d' ' -f2| cut -d':' -f1` \
    && echo 'type=83' | sfdisk $DISK_NAME \
    && echo -e 'y' | mkfs.ext4 $DISK_NAME \
    && mkdir -p /datadrive \
    && mount $DISK_NAME /datadrive \
    && mkdir -p /datadrive/images \
    && chmod 777 /datadrive/images

# Install and configure Azure Security Pack - https://microsoft.sharepoint.com/:o:/r/teams/AzureSecurityCompliance/Security/AzSecPack/Azure%20Security%20Pack?d=w6542d8d972f4489db779ad7b27a1892e&csf=1&e=PmoHLc
echo 'deb [arch=amd64] http://packages.microsoft.com/repos/azurecore/ bionic main' | tee -a /etc/apt/sources.list.d/azure.list
curl https://packages.microsoft.com/keys/msopentech.asc | apt-key add -
apt-get -yq update
apt-get install -y azure-mdsd azure-security azsec-monitor azsec-clamav
wget -O /etc/mdsd.d/mdsd.xml https://linuxazsecpackfiles.blob.core.windows.net/files/mdsd.xml
sed -i -e "s/NAMESPACE/$SCRIPT_PARAM_GCS_NAMESPACE/g" /etc/mdsd.d/mdsd.xml
sed -i -e "s/MONIKER/$SCRIPT_PARAM_GCS_ROLE/g" /etc/mdsd.d/mdsd.xml
sed -i -e "s/SUBSCRIPTION_ID/$SCRIPT_PARAM_GCS_TENANT/g" /etc/mdsd.d/mdsd.xml
sed -i -e "s/RESOURCE_GROUP_NAME/$SCRIPT_PARAM_GCS_ROLE/g" /etc/mdsd.d/mdsd.xml
gcs_tempfile=$(mktemp)
base64 --decode <<< $SCRIPT_PARAM_GCS_PFX_BASE64 > $gcs_tempfile
passwordplaintext=""
openssl pkcs12 -in $gcs_tempfile -out /etc/mdsd.d/gcscert.pem -nodes -nokeys -clcerts -password pass:$passwordplaintext
openssl pkcs12 -in $gcs_tempfile -out /etc/mdsd.d/gcskey.pem -nodes -nocerts -clcerts -password pass:$passwordplaintext
rm $gcs_tempfile
echo "MDSD_ROLE_PREFIX=/var/run/mdsd/default
MDSD_OPTIONS=\"-d -A -r \${MDSD_ROLE_PREFIX}\"
MDSD_LOG=/var/log
MDSD_SPOOL_DIRECTORY=/var/opt/microsoft/linuxmonagent
MDSD_OPTIONS=\"-A -c /etc/mdsd.d/mdsd.xml -d -r \$MDSD_ROLE_PREFIX -S \$MDSD_SPOOL_DIRECTORY/eh -e \$MDSD_LOG/mdsd.err -w \$MDSD_LOG/mdsd.warn -o \$MDSD_LOG/mdsd.info\"
export SSL_CERT_DIR=/etc/ssl/certs
export MONITORING_GCS_ENVIRONMENT=$SCRIPT_PARAM_GCS_ENVIRONMENT
export MONITORING_GCS_ACCOUNT=$SCRIPT_PARAM_GCS_ACCOUNT
imdsURL=\"http://169.254.169.254/metadata/instance/compute/location?api-version=2017-04-02&format=text\"
export MONITORING_GCS_REGION=\"\$(curl -H Metadata:True --silent \$imdsURL)\"
export MONITORING_GCS_CERT_CERTFILE=/etc/mdsd.d/gcscert.pem
export MONITORING_GCS_CERT_KEYFILE=/etc/mdsd.d/gcskey.pem" > /etc/default/mdsd
source /etc/default/mdsd
chown syslog /etc/mdsd.d/gcscert.pem
chown syslog /etc/mdsd.d/gcskey.pem
chmod 400 /etc/mdsd.d/gcscert.pem
chmod 400 /etc/mdsd.d/gcskey.pem
azsecd config -s baseline -d P1D
azsecd config -s software -d P1D
azsecd config -s clamav -d P1D
service mdsd restart
service azsecd restart

echo "Done."

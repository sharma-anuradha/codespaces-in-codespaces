#!/bin/bash

root_out="/mnt/cloudenvdata/log"
custom=( )

while [[ $# -gt 0 ]]
do
    case "$1" in
        -o|--out) shift
            root_out="$1";;
		-e|--extra) shift
            custom+=("$1");;
    esac
    shift # next argument
done

# Setup root directory
mkdir -p $root_out

# Sys logs
[[ -e /var/log/syslog ]] && cp /var/log/syslog ${root_out}/syslog
[[ -e /var/log/kern.log ]] && cp /var/log/kern.log ${root_out}/kern.log
journalctl -k > ${root_out}/journal.log

# Docker logs
mkdir -p ${root_out}/docker
docker ps -aq | while read id ; do docker inspect $id > ${root_out}/docker/${id}.inspect.json ; done

# Audit logs
mkdir -p ${root_out}/auditd
ausearch -k audit_kill > ${root_out}/auditd/kill.txt

# Codespaces logs
mkdir -p ${root_out}/containerTmp
[[ -e /mnt/containerTmp/VSFeedbackVSRTCLogs ]] && cp -r /mnt/containerTmp/VSFeedbackVSRTCLogs ${root_out}/containerTmp/VSFeedbackVSRTCLogs
[[ -e /mnt/containerTmp/vsls-agent ]] && cp -r /mnt/containerTmp/vsls-agent ${root_out}/containerTmp/vsls-agent

# Custom logs
[[ -e /tmp/top_logs.txt ]] && cp /tmp/top_logs.txt ${root_out}/top_logs.txt

for i in "${custom[@]}"
do
    [[ -f $i ]] && mkdir -p "$(dirname ${root_out}/$i)" && cp $i ${root_out}/$i
    [[ -d $i ]] && cp -r $i ${root_out}/$i
done
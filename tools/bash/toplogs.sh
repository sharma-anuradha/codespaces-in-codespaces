#!/bin/bash

out="/tmp/top_logs.txt"
rate="0.1"

while [[ $# -gt 0 ]]
do
    case "$1" in
        -o|--out) shift
            out="$1";;
        -r|--rate) shift
            rate="$1";;
    esac
    shift # next argument
done

touch $out
while sleep $rate; do
	date --utc +"%d/%m/%Y %T.%N" >> $out
	top -b -n1 >> $out
	echo --- >> $out
done
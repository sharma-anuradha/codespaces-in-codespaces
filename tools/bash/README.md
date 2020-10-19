## Top Logs

Periodically runs `top` and dumps to a file. Useful for performance/usage monitoring.

Logs can be parsed using the toplogs.exe from `$REPO/tools/TopLogs` project. See [`$REPO/tools/TopLogs/README.md`](../TopLogs/README.md).

### Usage

(Recommended) Run in background
```sh
./toplogs.sh &
```

Output to `stdout` instead of a log file:
```sh
./toplogs.sh --out /dev/stdout
```

Run ever 10 seconds:
```sh
./toplogs.sh --rate 10
```

### Options
```
    -o | --out      (Default: "/tmp/top_logs.txt") Output log file
    
    -r | --rate     (Default: 0.1) Output rate in seconds
```

### Notes
It is useful to run this as a background process so that you can keep using the VM in the meantime (**it will not block a Codespace using the VM in any case**). To stop the background process you can either:

- Find its `pid` using `ps` and kill it using `kill`
- Bring it to the foreground using `fg` and kill it with `Ctrl+C`

---

## Dump Logs

Copies a set of common VM and Codespace logs to a common folder. Useful for quickly grabbing logs and moving to a backup location (e.g. a file share) before a VM is deleted.

### Usage:

(Recommended) Dump logs to already mounted Codespace file share (`/mnt/cloudenvdata`) in a new `log` directory
```sh
./dumplogs.sh
```

Dump to a specific directory
```sh
./dumplogs.sh --out /tmp/logdump
```

Also dump one or more custom logs (these logs are output to `{root-output}/{extra-path}`, e.g. `/mnt/cloudenvdata/log/my/extra/log.txt`)
```sh
./dumplogs.sh --extra my/extra/log.txt --extra my/other/log.txt
```

### Options
```
    -o | --out      (Default: "/mnt/cloudenvdata/log") Output directory
    
    -e | --extra    Extra log files to copy (can be specified multiple times)
```
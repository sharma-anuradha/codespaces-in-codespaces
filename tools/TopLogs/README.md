# Top Logs

Used to parse log files produced by [`$REPO/tools/bash/toplogs.sh`](../bash/README.md).

Reference the exe's `help` option for basic details.

## Usage

### show-table

Create a TSV of top level CPU and Memory metrics (pipe to `clip` to copy directly to Excel)
```sh
.\toplogs.exe show-table -i "path\to\top_logs.txt"

OUTPUT (with improved formatting, output is actually a TSV):

TIME                    MS    USERS   TASKS   %CPU   USER CPU   SYSTEM CPU   %MEM      USED MEM   FREE      AVAIL MEM
10/12/2020 1:14:42 PM   0     1       1       0      2          1.3          10.8999   277940     2677748   3513216
10/12/2020 1:14:43 PM   282   1       1       0      2          1.3          10.8999   277940     2677740   3513216
10/12/2020 1:14:43 PM   545   1       1       0      2          1.3          10.8999   277920     2677748   3513232
... more rows ...
```

### find-proc

Search for all process named `codespaces`
```sh
.\toplogs.exe find-proc -i "path\to\top_logs.txt" --command "codespaces"

OUTPUT (with improved formatting, output is actually a TSV):

PID    COMMAND      USER       START                   END                     MIN CPU   MAX CPU   MIN MEM   MAX MEM
2022   codespaces   root       10/12/2020 1:14:43 PM   10/12/2020 1:18:23 PM   0         113.3     2.1       2.9
4814   codespaces   cloudenv   10/12/2020 1:15:32 PM   10/12/2020 1:18:23 PM   0         87.5      0         1.8
5021   codespaces   cloudenv   10/12/2020 1:15:38 PM   10/12/2020 1:16:44 PM   0         106.7     0.9       1.9
5383   codespaces   cloudenv   10/12/2020 1:15:42 PM   10/12/2020 1:15:42 PM   113.3     113.3     1.1       1.1
```

Search for all process named `codespaces` which use over 100% CPU
```sh
.\toplogs.exe find-proc -i "path\to\top_logs.txt" --command "codespaces" --cpu-max ">100"

OUTPUT (with improved formatting, output is actually a TSV):

PID    COMMAND      USER       START                   END                     MIN CPU   MAX CPU   MIN MEM   MAX MEM
2022   codespaces   root       10/12/2020 1:14:43 PM   10/12/2020 1:18:23 PM   0         113.3     2.1       2.9
5383   codespaces   cloudenv   10/12/2020 1:15:42 PM   10/12/2020 1:15:42 PM   113.3     113.3     1.1       1.1
```

Search for all process named `codespaces` ending at or before "10/12/2020 1:16:44 PM"
```sh
.\toplogs.exe find-proc -i "path\to\top_logs.txt" --command "codespaces" --end-time "<=10/12/2020 1:16:44 PM"

OUTPUT (with improved formatting, output is actually a TSV):

PID    COMMAND      USER       START                   END                     MIN CPU   MAX CPU   MIN MEM   MAX MEM
5021   codespaces   cloudenv   10/12/2020 1:15:38 PM   10/12/2020 1:16:44 PM   0         106.7     0.9       1.9
5383   codespaces   cloudenv   10/12/2020 1:15:42 PM   10/12/2020 1:15:42 PM   113.3     113.3     1.1       1.1
```

Search for all process named `codespaces`, only output PID
```sh
.\toplogs.exe find-proc -i "path\to\top_logs.txt" --command "codespaces" --cpu-max ">100" -q

OUTPUT:

2022
4814
5021
5383
```

### proc-details

Show details of `PID=2022`
```sh
.\toplogs.exe proc-details -i "path\to\top_logs.txt" --pid 2022

OUTPUT (with improved formatting, output table is actually a TSV):

Pid: 2022
Command: codespaces
User: root
%Cpu: min=0, max=113.3
%Mem: min=2.1, max=2.9

TIME                    MS      CPU     MEM
10/12/2020 1:14:42 PM   0       0       2.1
10/12/2020 1:14:43 PM   282     0       2.1
10/12/2020 1:14:43 PM   545     0       2.1
10/12/2020 1:14:43 PM   807     0       2.1
10/12/2020 1:14:44 PM   1069    0       2.1
10/12/2020 1:14:44 PM   1332    0       2.1
... more rows ...
```

### Chaining commands

Show details of all process named `codepsaces`
```sh
.\toplogs.exe find-proc -i "path\to\top_logs.txt" --command codespaces -q |
   ForEach-Object { echo "---"; .\toplogs.exe proc-details -i "path\to\top_logs.txt" -p $_ }

OUTPUT (with improved formatting, output table is actually a TSV):

---
Pid: 2022
Command: codespaces
User: root
%Cpu: min=0, max=113.3
%Mem: min=2.1, max=2.9

TIME                    MS      CPU     MEM
10/12/2020 1:14:42 PM   0       0       2.1
10/12/2020 1:14:43 PM   282     0       2.1
10/12/2020 1:14:43 PM   545     0       2.1
10/12/2020 1:14:43 PM   807     0       2.1
10/12/2020 1:14:44 PM   1069    0       2.1
10/12/2020 1:14:44 PM   1332    0       2.1
... more rows ...
---
Pid: 4814
Command: codespaces
User: cloudenv
%Cpu: min=0, max=87.5
%Mem: min=0, max=1.8

TIME                    MS      CPU     MEM
10/12/2020 1:15:32 PM   0       0       0
10/12/2020 1:15:32 PM   265     6.7     0.2
10/12/2020 1:15:32 PM   529     6.2     0.6
10/12/2020 1:15:33 PM   793     31.2    0.9
... more rows ...
---
... other processes ...
```
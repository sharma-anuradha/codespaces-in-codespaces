# Backend/Frontend Rename

> **NOTE**: Migration scripts should not be run from within this repository.
The entire `migrations` folder should be copied outside the repo, and those
copies are what should be run. Running these scripts will switch branches
and perform several actions which could cause the current migration script
to disappear.

## migrate.ps1

A simple migration that renames `src\Backend` and `src\Resources`, as well as
deleting the obsolete `src\services\lib`. Not intended for general use.

## pr_migrate.ps1

Once the above changes have merged into `master`, updating existing PR branches
is best accomplished with this script. Run it without arguments for help.

## Requirements

`ripgrep` powers the repo-wide searching required by these scripts.
Try `choco install ripgrep` or see https://github.com/BurntSushi/ripgrep#installation for other options.
Note that the command installed into the path is actually `rg`.
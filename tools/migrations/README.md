# Repository Migration Tooling

This directory contains scripts used to assist with repository restructuring
efforts. For any disruptive restructuring, tooling is provided to allow PR
branch owners to update their branches in scenarios where a simple merge or
rebase is unrealistic.

> **NOTE**: Migration scripts should not be run from within this repository.
The entire `migrations` folder should be copied outside the repo, and those
copies are what should be run. Running these scripts can switch branches
and perform several actions which could cause the current migration script
to disappear.

## Migrations

Migration scripts should be placed in subdirectories named `{NN}-{name}`, where
`{NN}` is the sequential number of the migration, to keep them clearly ordered.

## Modules

The `modules` directory contains reusable components to help with common tasks
such as repo-wide find/replace.

Migration tooling is meant to be short-lived. Modules should not be expected to
maintain compatibility for older migration scripts. If you find yourself
needing to use an older migration script, you may want to look at a version of
this folder from the time it was still in use.
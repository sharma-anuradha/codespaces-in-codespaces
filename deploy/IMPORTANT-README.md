# CLUSTER DIRECTORY IS READ-ONLY

Please treat the `cluster` subdirectory as read-only. It is a `git subtree` used to inject external code into this repository.

Updates to `cluster` should be made in the remote `vsclk-cluster` repo and merged locally by calling `update-subtree-cluster.cmd`.

The command `update-subtree-cluster` will add the remote and the subtree if they are not already present. Otherwise, it will simply merge `cluster` directory from `vsclk-cluster`.

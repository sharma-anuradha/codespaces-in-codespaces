#!/bin/bash

./deploy-cluster.sh --dry-run --subscription vsclk-core-dev --prefix vsclk --name online --env dev --instance ci --stamp usw2 --stamp-location WestUs2 --location EastUs --team-group-id dry-run-group-id --ssl-cert-kv-name dry-run-ssl-cert-kv --cluster-version 1.0 --cluster-node-count eyAidXN3MiI6IDEsICJ1c2UiOiAxLCAiZXV3IjogMSwgImFzc2UiOiAxfQ== --signlr-enabled false --storage-account-prefix vso --dns-name dry-run-dns-name $@


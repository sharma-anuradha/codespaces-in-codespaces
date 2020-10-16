# How To Update Geneva Configuration

The geneva configuration is stored online at [Jarvis | Manage | Logs | Configurations](https://jarvis-west.dc.ad.msft.net/settings/mds).

- Log Endpoint: `Diagnostics PROD`
- Logs Accounts: `VsOnlineDev`, `VsOnlinePpe`, `VsOnlineProd`

The latest copy of the online configuration should be downloaded and checked in for reference and comparision.

- [VsOnlineDevVer1v0.xml](./VsOnlineDevVer1v0.xml)
- [VsOnlinePpeVer1v0.xml](./VsOnlinePpeVer1v0.xml)
- [VsOnlinePpeVer1v0.xml](./VsOnlineProdVer1v0.xml)

Our clusters require a matching copy of the Geneval config file. We use the template file [geneva-server.xml](./geneva-server.xml) to generate environment specific versions that we push to the clusters.

## Update Instructions

1. Start a new topic branch to manage the config changes.
1. Update [geneva-server.xml](./geneva-server.xml) with the desired updates.
    1. Verify your local changes by diffing [geneva-server.xml](./geneva-server.xml) against any/all of the checked in config files.
1. Go [Jarvis | Manage | Logs | Configurations](https://jarvis-west.dc.ad.msft.net/settings/mds)
1. Select log endpoint `Diagnostics PROD` and select the desired VsOnline log account.
1. Click on the desired log file link to open the warm path configuration.
1. Click on the "Advanced" tab.
1. Apply the same config changes manually.
    1. Click "Verify".
    1. Click "Save".
    1. Download and save the updated xml config file under the `config-files` directory.
    1. Verify your local changes by diffing [geneva-server.xml](./geneva-server.xml) against the udpated config files.
1. Repeat these steps for all three log accounts.
1. Create and commit the PR.
1. The cluster deployment scripts will update the geneva configuration in our clusters.

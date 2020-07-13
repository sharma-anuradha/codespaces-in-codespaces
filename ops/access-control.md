# Access Control

Production and non-production sysrtms use two different systems for access control.

## Access to PPE and PROD Subscriptions

Production and pre-production subscriptions live in the PME tenant and are governed by PME security groups defined in [OneIdentity](https://oneidentity.core.windows.net). You must sign in using your PME identity and Yubi key to obtain access to these groups.

Codespaces have these security groups in the PME tenant:

- `PME\AD-Codespaces` -- Administrators may manage JIT policy and approve JIT requests.
- `PME\TM-Codespaces` -- Team members may request JIT. Includes members of `AD-Codespaces`.
- `PME\AP-Codespaces` -- Approvers may approve service deployments and configuration changes. Includes members of `AD-Codespaces`.
- `PME\BG-Codespaces` -- Break-glass members may use [SAW Break Glass](https://aka.ms/sawbreakglass) for emergency access when JIT is not possible. This requires an `ALT_` identity.

>_These groups are defined per [this guidance](https://dev.azure.com/msazure/AzureWiki/_wiki/wikis/AzureWiki.wiki/29763/Security-Groups)._

### How to JIT into PME Subscriptions

PPE and PROD subscriptions are accessible only via JIT. They are **not visible** in the Auzre portal prior to JIT (per FedRamp security policy).

- See the [subscriptions list](./subscriptions.md) to determine which subscription ID you need.
- Go to [JIT Access](https://aka.ms/jitaccess) to request JIT and sign in using your PME identity.

## Access to DEV Subscriptions

Access to non-production subscription is controlled via the legacy [MyAccess project](https://aka.ms/myaccess) `vsclk-core`.

- Administrators group
- Team group
- Team-readonly group
- Approvers group
- Break Glass group

No JIT access is required for Development subscriptions. JIT access is required for our lecacy production subscriptions in the CORP tenant.

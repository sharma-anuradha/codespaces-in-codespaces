// GenevaValues.ts

import { IPlaneNames } from "./ResourceNameDefs";

// Latest values from
// https://genevamondocs.azurewebsites.net/collect/references/linuxcontainers.html
const acsKeyVaultAgentTag = "master_28"
const genevaFluentdTdAgentTag = "master_148"
const genevaMdmTag = "master_48"
const genevaMdsdTag = "master_309"
const genevaSecpackInstallTag = "master_57"

// Corp or AME tenant depends on environment.
// TODO: Long term this ought to be in environments.json.
const corpTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
const ameTenantId = "33e01921-4d64-4f8c-a055-5bdaffd5e33d";

export interface IGenevaValues {
    readonly acsKeyVaultAgentTag: string;
    readonly azSecPackAuditMoniker: string;
    readonly azSecPackDiagnosticsMoniker: string;
    readonly azSecPackEventVersion: string;
    readonly azSecPackGcsAccount: string;
    readonly azSecPackGcsCert: string;
    readonly azSecPackGcsCertName: string;
    readonly azSecPackGcsEnvironment: string;
    readonly azSecPackGcsKey: string;
    readonly azSecPackNamespace: string;
    readonly azSecPackRole: string;
    readonly azSecPackSecurityMoniker: string;
    readonly azSecPackTenant: string;
    readonly azSecPackTimestamp: string;
    readonly genevaFluentdTdAgentTag: string;
    readonly genevaMdmAccount: string;
    readonly genevaMdmTag: string;
    readonly genevaMdsdTag: string;
    readonly genevaSecpackInstallTag: string;

}

export abstract class GenevaValues {

    public static assignValues(plane: IPlaneNames): IGenevaValues {
        const env = plane.env.toLowerCase();
        const component = plane.component.toLowerCase();
        const Env = env.charAt(0).toUpperCase() + env.slice(1);
        const tenantId = (env === "dev") ? corpTenantId : ameTenantId;
        const genevaMdmAccount = `VsOnline${Env}`;

        if (plane.component === "codesp") {

            if (plane.plane === "ctl") {
                const values: IGenevaValues = {
                    azSecPackGcsCertName: `vsclk-${component}-${env}-monitoring`,
                    azSecPackGcsCert: `/secrets/certs/vsclk-${component}-${env}-monitoring`,
                    azSecPackGcsKey: `/secrets/keys/vsclk-${component}-${env}-monitoring`,
                    azSecPackGcsEnvironment: "DiagnosticsProd",
                    azSecPackGcsAccount: genevaMdmAccount,
                    azSecPackEventVersion: "1",
                    azSecPackTimestamp: "2018-05-07T00:00:00Z",
                    azSecPackNamespace: genevaMdmAccount,
                    azSecPackAuditMoniker: `vsonline${env}audit`,
                    azSecPackDiagnosticsMoniker: `vsonline${env}diag`,
                    azSecPackSecurityMoniker: `vsonline${env}security`,
                    azSecPackTenant: tenantId,
                    azSecPackRole: `vsonline${env}security`,
                    genevaMdmAccount: genevaMdmAccount,
                    acsKeyVaultAgentTag: acsKeyVaultAgentTag,
                    genevaFluentdTdAgentTag: genevaFluentdTdAgentTag,
                    genevaMdmTag: genevaMdmTag,
                    genevaMdsdTag: genevaMdsdTag,
                    genevaSecpackInstallTag: genevaSecpackInstallTag
                };

                Object.assign(plane, values);
                return values;
            }

        }

        return null;
    }

}

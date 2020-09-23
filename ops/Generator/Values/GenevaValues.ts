// GenevaValues.ts

export class GenevaValues {
    readonly acsKeyVaultAgentTag: string; // master_24
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

    private assignFunc: (target: unknown) => void;

    constructor(plane: string, component: string, env: string) {
        env = env.toLowerCase();
        const corpTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        const ameTenantId = "33e01921-4d64-4f8c-a055-5bdaffd5e33d";
        const tenantId = (env === "dev") ? corpTenantId : ameTenantId;
        const genevaMdmAccount = GenevaValues.getGenevaMdmAccount(env);

        if (component === "core") {
            if (plane === "ctl") {
                this.azSecPackGcsCertName = `vsclk-core-${env}-monitoring`;
                this.azSecPackGcsCert = `/secrets/certs/vsclk-core-${env}-monitoring`,
                    this.azSecPackGcsKey = `/secrets/keys/vsclk-core-${env}-monitoring`,
                    this.azSecPackGcsEnvironment = "DiagnosticsProd",
                    this.azSecPackGcsAccount = genevaMdmAccount,
                    this.azSecPackEventVersion = "1",
                    this.azSecPackTimestamp = "2018-05-07T00:00:00Z",
                    this.azSecPackNamespace = genevaMdmAccount,
                    this.azSecPackAuditMoniker = `vsonline${env}audit`,
                    this.azSecPackDiagnosticsMoniker = `vsonline${env}diag`,
                    this.azSecPackSecurityMoniker = `vsonline${env}security`,
                    this.azSecPackTenant = tenantId,
                    this.azSecPackRole = `vsonline${env}security`,
                    this.genevaMdmAccount = genevaMdmAccount
            }

            // Latest values from
            // https://genevamondocs.azurewebsites.net/collect/references/linuxcontainers.html
            // Used in both ops and control planes.
            this.acsKeyVaultAgentTag = "master_28"
            this.genevaFluentdTdAgentTag = "master_148"
            this.genevaMdmTag = "master_48"
            this.genevaMdsdTag = "master_309"
            this.genevaSecpackInstallTag = "master_57"

            // Only need values for non-data planes
            if (plane !== "data") {
                this.assignFunc = (target: unknown) => {
                    Object.assign(target, this);
                }
            }
        }
    }

    private static getGenevaMdmAccount(env: string): string {
        env = env.toLowerCase();
        const Env = env.charAt(0).toUpperCase() + env.slice(1);
        return `VsOnline${Env}`;
    }

    assignValues(target: unknown): void {
        if (this.assignFunc) {
            this.assignFunc(target);
        }
    }
}

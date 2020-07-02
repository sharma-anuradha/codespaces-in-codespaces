const CODESPACES_NAME = 'Microsoft_Codespaces';

export let logger: MsPortalFx.Base.Diagnostics.Log;

export function initializeLogger() {
    MsPortalFx.Base.Diagnostics.Telemetry.initialize(CODESPACES_NAME);

    logger = new MsPortalFx.Base.Diagnostics.Log(CODESPACES_NAME);
}

export function trace(source: string, action: string, data?: any) {
    MsPortalFx.Base.Diagnostics.Telemetry.trace({
        extension: CODESPACES_NAME,
        source,
        action,
        data,
    });
}

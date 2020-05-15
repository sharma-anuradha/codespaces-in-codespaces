import { TelemetryService } from 'vso-client-core';
import { getVSCodeVersion } from 'vso-workbench';

export interface IActionTelemetryProperties {
    action: string;
    correlationId: string;
    isInternal: boolean;
}

export { sendTelemetry } from './sendTelemetry';

const vscodeConfig = getVSCodeVersion();

export const telemetry = new TelemetryService({
    portalVersion: process.env.PORTAL_VERSION,
    vscodeCommit: vscodeConfig.commit,
    vscodeQuality: vscodeConfig.quality,
});

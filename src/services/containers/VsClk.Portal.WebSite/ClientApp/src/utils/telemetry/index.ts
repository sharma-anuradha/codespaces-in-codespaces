import { TelemetryService } from 'vso-client-core';
import { getVSCodeVersion } from 'vso-workbench';

import versionFile from '../../version.json';

export interface IActionTelemetryProperties {
    action: string;
    correlationId: string;
    isInternal: boolean;
}

export { sendTelemetry } from './sendTelemetry';

const vscodeConfig = getVSCodeVersion();

export const telemetry = new TelemetryService({
    portalVersion: versionFile.version,
    vscodeCommit: vscodeConfig.commit,
    vscodeQuality: vscodeConfig.quality,
});

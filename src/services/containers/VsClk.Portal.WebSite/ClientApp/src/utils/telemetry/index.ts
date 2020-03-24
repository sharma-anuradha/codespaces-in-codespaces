export interface IActionTelemetryProperties {
    action: string;
    correlationId: string;
    isInternal: boolean;
}
import { TelemetryService } from 'vso-client-core';

export { sendTelemetry } from './sendTelemetry';
import { getVSCodeVersion } from '../featureSet';
import versionFile from '../../version.json';

const vscodeConfig = getVSCodeVersion();

export const telemetry = new TelemetryService({
    portalVersion: versionFile.version,
    vscodeCommit: vscodeConfig.commit,
    vscodeQuality: vscodeConfig.quality,
});

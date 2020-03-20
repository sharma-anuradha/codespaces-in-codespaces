export interface IActionTelemetryProperties {
    action: string;
    correlationId: string;
    isInternal: boolean;
}

export { sendTelemetry } from './sendTelemetry';

import { TelemetryService } from 'vso-client-core';
import { getVSCodeVersion } from '../../constants';

import versionFile from '../../version.json';
import { getFeatureSet } from '../featureSet';

const vscodeConfig = getVSCodeVersion(getFeatureSet());

export const telemetry = new TelemetryService({
    portalVersion: versionFile.version,
    vscodeCommit: vscodeConfig.commit,
    vscodeQuality: vscodeConfig.quality
});

import { TelemetryService } from 'vso-client-core';

import { getVSCodeVersion } from '../utils/getVSCodeVersion';
export { sendTelemetry } from './sendTelemetry';

import packageJSON from '../../package.json';

const vscodeConfig = getVSCodeVersion();

export const telemetry = new TelemetryService({
    portalName: packageJSON.name,
    portalVersion: packageJSON.version,
    vscodeCommit: vscodeConfig.commit,
    vscodeQuality: vscodeConfig.quality,
});

export interface IActionTelemetryProperties {
    action: string;
    correlationId: string;
    isInternal: boolean;
}

export { sendTelemetry } from './sendTelemetry';

import { telemetry as telemetryObject } from './TelemetryService';

export const initTelemetry = telemetryObject.initializeTelemetry;
export const telemetry = telemetryObject.instance;

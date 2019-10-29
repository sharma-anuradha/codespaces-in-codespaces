export interface IActionTelemetryProperties {
    action: string;
    correlationId: string;
    isInternal: boolean;
}

export { sendTelemetry } from './sendTelemetry';
export { telemetryCore as telemetry } from './TelemetryService';

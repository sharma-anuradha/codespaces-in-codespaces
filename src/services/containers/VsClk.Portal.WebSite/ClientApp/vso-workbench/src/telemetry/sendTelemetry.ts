import { ITelemetryEvent, cleanupPIIForExternal, TelemetryPropertyValue } from 'vso-client-core';

import { telemetry } from './telemetry';
import { authService } from '../auth/authService';
import { IVSOWorkbenchError } from '../interfaces/IVSOWorkbenchError';

export class ExceptionTelemetryEvent implements ITelemetryEvent {
    constructor(
        readonly name: string = 'vso/workbench/error',
        private readonly error: IVSOWorkbenchError
    ) {}

    get properties() {
        const error = this.error;

        const { name, message, stack, errorType = 'unknown type' } = error;

        return {
            stack: cleanupPIIForExternal(authService.isInternal, stack),
            message,
            name,
            type: errorType,
        };
    }
}

export class PropertiesTelemetryEvent implements ITelemetryEvent {
    constructor(
        public readonly name: string,
        public readonly properties: Record<string, TelemetryPropertyValue>
    ) {}
}

type SendTelemetryProps =
    | ['vsonline/workbench/resolve-external-uri', { port: number }]
    | ['vsonline/workbench/error', IVSOWorkbenchError];

let isTelemetryInitialized = false;
export function sendTelemetry(...args: SendTelemetryProps): void;
export function sendTelemetry(telemetryEventName: any, properties: any) {
    const event =
        properties instanceof Error
            ? new ExceptionTelemetryEvent(telemetryEventName, properties)
            : new PropertiesTelemetryEvent(telemetryEventName, properties);

    if (!isTelemetryInitialized) {
        telemetry.initializeTelemetry((_: string) => null);
        isTelemetryInitialized = true;
    }

    telemetry.track(event);
}

import { ITelemetryEvent, cleanupPIIForExternal, TelemetryPropertyValue, EnvironmentType } from 'vso-client-core';

import { telemetry } from './telemetry';
import { authService } from '../auth/authService';
import { IVSOWorkbenchError } from '../interfaces/IVSOWorkbenchError';

export class ExceptionTelemetryEvent implements ITelemetryEvent {
    constructor(
        readonly name: string = 'vso/workbench/error',
        private readonly error: IVSOWorkbenchError
    ) { }

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
    ) { }
}



interface IVSCodeConnectProperties {
    connectionCorrelationId: string;
    isFirstConnection: boolean;
    connectionNumber: number;
    environmentType: EnvironmentType;
}

interface IVSCodeConnectProperties {
    connectionCorrelationId: string;
    isFirstConnection: boolean;
    connectionNumber: number;
    environmentType: EnvironmentType;
}

interface IVSCodeConnectWithDurationProperties extends IVSCodeConnectProperties {
    duration: number;
}

interface IVSCodeConnectionFailedProperties extends IVSCodeConnectWithDurationProperties {
    retry: number;
    error: Error;
}

interface IVSCodeConnectPageReloadProperties extends IVSCodeConnectProperties {
    retry: number;
}

type SendTelemetryProps =
    | ['vsonline/workbench/resolve-external-uri', { port: number }]
    | ['vsonline/workbench/error', IVSOWorkbenchError]
    | ['vsonline/portal/ls-connection-page-reload', IVSCodeConnectPageReloadProperties]
    | ['vsonline/portal/ls-connection-failed', IVSCodeConnectionFailedProperties]
    | ['vsonline/portal/ls-connection-opened', IVSCodeConnectWithDurationProperties]
    | ['vsonline/portal/ls-connection-initializing', IVSCodeConnectProperties]
    | ['vsonline/portal/ls-connection-close', IVSCodeConnectProperties]
    | ['vsonline/portal/connect-with-retry', { correlationId: string; duration: number }]
    | ['vsonline/portal/vscode-time-to-interactive', { hostedOn: string; duration: number }]
    | ['vsonline/extensionload/error', Error];

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

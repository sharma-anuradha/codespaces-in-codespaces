import {
    ITelemetryEvent,
    TelemetryPropertyValue,
    EnvironmentType,
} from 'vso-client-core';

import { telemetry } from './telemetry';

export class PropertiesTelemetryEvent implements ITelemetryEvent {
    constructor(
        public readonly name: string,
        public readonly properties: Record<string, TelemetryPropertyValue>
    ) {}
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
    | ['vsonline/portal/ls-connection-page-reload', IVSCodeConnectPageReloadProperties]
    | ['vsonline/portal/ls-connection-failed', IVSCodeConnectionFailedProperties]
    | ['vsonline/portal/ls-connection-opened', IVSCodeConnectWithDurationProperties]
    | ['vsonline/portal/ls-connection-initializing', IVSCodeConnectProperties]
    | ['vsonline/portal/ls-connection-close', IVSCodeConnectProperties]
    | ['vsonline/portal/connect-with-retry', { correlationId: string; duration: number }];



let isTelemetryInitialized = false;
export function sendTelemetry(...args: SendTelemetryProps): void;
export function sendTelemetry(telemetryEventName: any, properties: any) {
    const event = new PropertiesTelemetryEvent(telemetryEventName, properties);

    if (!isTelemetryInitialized) {
        telemetry.initializeTelemetry((_: string) => null );
        isTelemetryInitialized = true;
    }

    telemetry.track(event);
}

import { ITelemetryEvent, cleanupPIIForExternal, TelemetryPropertyValue, EnvironmentType, TCodespaceEnvironment } from 'vso-client-core';

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

interface ISubdomainMismatchProperties {
    topLevelDomain: string;
    environment: TCodespaceEnvironment;
}

export interface ITimeBlock {
    startTime: number | null;
    duration: number | null;
}

export interface ITelementryStartupTimes {
    timeToJavascript: number | null;
    timeToTerminal: number | null;
    workbenchPageInitTime: number | null;
    startCodespaceTime: number | null;
    timeToVSCode: number | null;
    getEnvironmentInfo1Time: number | null;
    getEnvironmentInfo2Time: number | null;
    pureConnectionTime: number | null;
    vscodeServerStartupTime: number | null;
    clientServerHandshake: number | null;
    getLiveshareWorkspaceInfo: number | null;
    vscodeTime: ITimeBlock;
    workbenchComponentTime: ITimeBlock;
    workbenchPageTime: ITimeBlock;
}

type SendTelemetryProps =
    | ['vsonline/workbench/resolve-external-uri', { port: number }]
    | ['vsonline/workbench/error', IVSOWorkbenchError]
    | ['vsonline/workbench/bydesign-error/subdomain-mismatch', ISubdomainMismatchProperties]
    | ['vsonline/portal/ls-connection-page-reload', IVSCodeConnectPageReloadProperties]
    | ['vsonline/portal/ls-connection-failed', IVSCodeConnectionFailedProperties]
    | ['vsonline/portal/ls-connection-opened', IVSCodeConnectWithDurationProperties]
    | ['vsonline/portal/ls-connection-initializing', IVSCodeConnectProperties]
    | ['vsonline/portal/ls-connection-close', IVSCodeConnectProperties]
    | ['vsonline/portal/connect-with-retry', { correlationId: string; duration: number }]
    | ['vsonline/portal/vscode-time-to-interactive', { hostedOn: string; duration: number }]
    | ['vsonline/portal/startup-times', ITelementryStartupTimes]
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

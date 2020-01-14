import { PropertiesTelemetryEvent, ExceptionTelemetryEvent } from './TelemetryEvents';
import { EnvironmentType } from '../../interfaces/cloudenvironment';
import { telemetry } from '.';

type SendTelemetryProps =
    | ['vsonline/cipher/decrypt', IDecryptionTelemetryEventProperties]
    | ['vsonline/cipher/encrypt', IEncryptionTelemetryEventProperties]
    | ['vsonline/cipher/error', Error]
    | ['vsonline/cipher/no-decryption-key', INoDecryptionKeyTelemetryEventProperties]
    | ['vsonline/auth/acquire-token/error', Error]
    | ['vsonline/auth/acquire-auth-code', IAcquireAuthCodeTelemetryEventProperties]
    | ['vsonline/application/before-unload', {}]
    | ['vsonline/portal/resolve-external-uri', { port: number }]
    | ['vsonline/portal/ls-connection-initializing', IVSCodeConnectProperties]
    | ['vsonline/portal/ls-connection-opened', IVSCodeConnectWithDurationProperties]
    | ['vsonline/portal/ls-connection-failed', IVSCodeConnectionFailedProperties]
    | ['vsonline/portal/ls-connection-close', IVSCodeConnectProperties]
    | ['vsonline/request', IResponseProperties]
    | ['vsonline/extensionload/error', Error];

export function sendTelemetry(...args: SendTelemetryProps): void;
export function sendTelemetry(telemetryEventName: any, properties: any) {
    const event =
        properties instanceof Error
            ? new ExceptionTelemetryEvent(telemetryEventName, properties)
            : new PropertiesTelemetryEvent(telemetryEventName, properties);

    telemetry.track(event);
}

// Properties

interface IDecryptionTelemetryEventProperties {
    timeSpent: number;
    payloadLengthBefore: number;
    payloadLengthAfter: number;
}

interface IEncryptionTelemetryEventProperties {
    timeSpent: number;
    payloadLengthBefore: number;
    payloadLengthAfter: number;
}

interface INoDecryptionKeyTelemetryEventProperties {}
interface IAcquireAuthCodeTelemetryEventProperties {
    isCodeAcquired: boolean;
}

interface IResponseProperties {
    requestId: string;
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

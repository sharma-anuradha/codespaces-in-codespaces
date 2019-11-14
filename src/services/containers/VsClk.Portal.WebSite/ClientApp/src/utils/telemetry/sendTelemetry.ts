import { PropertiesTelemetryEvent, ExceptionTelemetryEvent } from './TelemetryEvents';
import { telemetryCore } from '../../utils/telemetry/TelemetryService';

type SendTelemetryProps =
    | ['vsonline/cipher/decrypt', IDecryptionTelemetryEventProperties]
    | ['vsonline/cipher/encrypt', IEncryptionTelemetryEventProperties]
    | ['vsonline/cipher/error', Error]
    | ['vsonline/cipher/no-decryption-key', INoDecryptionKeyTelemetryEventProperties]
    | ['vsonline/auth/acquire-token/error', Error]
    | ['vsonline/auth/acquire-auth-code', IAcquireAuthCodeTelemetryEventProperties]
    | ['vsonline/application/before-unload', {}]
    | ['vsonline/vscode/connect', IVSCodeConnectProperties]
    | ['vsonline/request', IResponseProperties];

export function sendTelemetry(...args: SendTelemetryProps): void;
export function sendTelemetry(telemetryEventName: any, properties: any) {
    const event =
        properties instanceof Error
            ? new ExceptionTelemetryEvent(telemetryEventName, properties)
            : new PropertiesTelemetryEvent(telemetryEventName, properties);

    telemetryCore.track(event);
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
}

import { TelemetryPropertyValue } from './ITelemetryEvent';

export interface ICustomTelemetryContextProperties {
    [key: string]: TelemetryPropertyValue;
}

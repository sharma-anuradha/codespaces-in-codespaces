export interface ITelemetryEvent {
    readonly name: string;
    readonly properties: Readonly<Record<string, TelemetryPropertyValue>>;
}

export type TelemetryPropertyValue = string | number | Date | boolean | Error | undefined | null;

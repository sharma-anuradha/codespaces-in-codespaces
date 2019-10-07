export interface ITelemetryEvent {
    readonly name: string;
    getProperties(
        defaultProperties: Record<string, TelemetryPropertyValue>
    ): Record<string, TelemetryPropertyValue>;
}

export type TelemetryPropertyValue = string | number | Date | boolean | Error | undefined | null;

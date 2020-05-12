import { TelemetryPropertyValue } from '../interfaces/ITelemetryEvent';

export function prefixPropertyNames(
    properties: Record<string, TelemetryPropertyValue>,
    prefix: string
): Record<string, TelemetryPropertyValue> {
    const keys = Object.keys(properties) as (keyof typeof properties)[];
    return keys.reduce((commonProperties, property) => {
        return {
            ...commonProperties,
            [`${prefix}.${property}`]: properties[property],
        };
    }, {} as Record<string, any>);
}

import moment from 'moment';
import { isNumber } from 'util';
import { prefixPropertyNames } from './prefixPropertyNames';
import { ITelemetryEvent, TelemetryPropertyValue } from '../interfaces/ITelemetryEvent';

export class TelemetryEventSerializer {
    serialize(
        event: ITelemetryEvent,
        defaultProperties: Record<string, TelemetryPropertyValue>,
        prefix = 'vsonline.portal.event'
    ): {
        properties: Record<string, string>;
        measurements: Record<string, number>;
    } {
        const properties: Record<string, string> = {};
        const measurements: Record<string, number> = {};
        const allData = {
            ...defaultProperties,
            ...(prefix ? prefixPropertyNames(event.properties, prefix) : event.properties),
        };
        for (const [name, value] of Object.entries(allData)) {
            if (value === undefined) {
                properties[name] = 'undefined';
            } else if (value === null) {
                properties[name] = 'null';
            } else if (isNumber(value)) {
                measurements[name] = value;
            } else if (value instanceof Date) {
                properties[name] = moment(value)
                    .utc()
                    .format('YYYY-MM-DD HH:mm:ss.SSS');
            } else if (value instanceof Error) {
                properties[name] = this.toSafeErrorMessage(value);
            } else if (typeof value === 'boolean') {
                properties[name] = value.toString();
            } else {
                properties[name] = value;
            }
        }
        return { properties, measurements };
    }
    private toSafeErrorMessage(error: Error) {
        // We handle error message PII on Nova.
        return error.message;
    }
}

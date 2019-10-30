import moment from 'moment';
import { isNumber } from 'util';
import { ITelemetryEvent, TelemetryPropertyValue } from './types';
export class TelemetryEventSerializer {
    serialize(
        event: ITelemetryEvent,
        defaultProperties: Record<string, TelemetryPropertyValue>
    ): {
        properties: Record<string, string>;
        measurements: Record<string, number>;
    } {
        const properties: Record<string, string> = {};
        const measurements: Record<string, number> = {};
        const allData = event.getProperties(defaultProperties);
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
                properties[name] = this.stripPii(value);
            } else if (typeof value === 'boolean') {
                properties[name] = value.toString();
            } else {
                properties[name] = value;
            }
        }
        return { properties, measurements };
    }
    private stripPii(error: Error) {
        return `Redacted <error: ${error.message.substr(0, 3)}>`;
    }
}

import { ITelemetryEvent, TelemetryPropertyValue } from './types';

export class ApplicationLoadEvent implements ITelemetryEvent {
    static readonly markName: string = 'vsonline/application/loaded';
    readonly name: string = ApplicationLoadEvent.markName;

    constructor() {}

    getProperties(defaultProperties: Record<string, TelemetryPropertyValue>) {
        const { requestStart, responseEnd } = window.performance.timing;

        const pageRequestDuration = responseEnd - requestStart;

        const [applicationLoadMark] = window.performance.getEntriesByName(
            ApplicationLoadEvent.markName
        );

        return {
            ...defaultProperties,

            pageRequestDuration,
            applicationLoadedDuration: applicationLoadMark.duration,
        };
    }
}

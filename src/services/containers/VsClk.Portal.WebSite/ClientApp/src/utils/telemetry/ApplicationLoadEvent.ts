import { ITelemetryEvent } from 'vso-client-core';

export class ApplicationLoadEvent implements ITelemetryEvent {
    static readonly markName: string = 'vsonline/application/loaded';
    readonly name: string = ApplicationLoadEvent.markName;

    constructor() {}

    get properties() {
        const { requestStart, responseEnd } = window.performance.timing;

        const pageRequestDuration = responseEnd - requestStart;

        const [applicationLoadMark] = window.performance.getEntriesByName(
            ApplicationLoadEvent.markName
        );

        return {
            pageRequestDuration,
            referrer: document.referrer,
            applicationLoadedDuration: applicationLoadMark.duration,
        };
    }
}

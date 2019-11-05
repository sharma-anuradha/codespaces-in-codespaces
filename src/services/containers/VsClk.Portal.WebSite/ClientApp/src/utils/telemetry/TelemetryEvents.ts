import { cleanupPIIForExternal } from '../cleanupPII';
import { exceptionEventName } from './TelemetryEventNames';

import { ITelemetryEvent, TelemetryPropertyValue } from './types';

export class ExceptionTelemetryEvent implements ITelemetryEvent {
    constructor(readonly name: string = exceptionEventName, private readonly error: Error) {}

    get properties() {
        const { name, message, stack } = this.error;

        return {
            stack: cleanupPIIForExternal(stack),
            message,
            name,
        };
    }
}

export class PropertiesTelemetryEvent implements ITelemetryEvent {
    constructor(
        public readonly name: string,
        public readonly properties: Record<string, TelemetryPropertyValue>
    ) {}
}

import {
    cleanupPIIForExternal,
    ITelemetryEvent,
    TelemetryPropertyValue
} from 'vso-client-core';
import { useActionContext } from '../../actions/middleware/useActionContext';

import { exceptionEventName } from './TelemetryEventNames';

export class ExceptionTelemetryEvent implements ITelemetryEvent {
    constructor(readonly name: string = exceptionEventName, private readonly error: Error) {}

    get properties() {
        const { name, message, stack } = this.error;

        const context = useActionContext();
        const { isInternal } = context.state.authentication;

        return {
            stack: cleanupPIIForExternal(isInternal, stack),
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

import { WithMetadata, ErrorAction } from '../../actions/middleware/types';
import { ITelemetryEvent, TelemetryPropertyValue } from './types';
export class ActionErrorEvent implements ITelemetryEvent {
    readonly name: string = 'vsonline/action/failure';
    constructor(
        private readonly action: WithMetadata<ErrorAction>,
        private readonly isInternal: boolean
    ) {}
    getProperties(
        defaultProperties: Record<string, TelemetryPropertyValue>
    ): Record<string, TelemetryPropertyValue> {
        return {
            ...defaultProperties,
            ...this.action.metadata.telemetryProperties,
            action: this.action.type,
            isInternal: this.isInternal,
            correlationId: this.action.metadata.correlationId,
        };
    }
}

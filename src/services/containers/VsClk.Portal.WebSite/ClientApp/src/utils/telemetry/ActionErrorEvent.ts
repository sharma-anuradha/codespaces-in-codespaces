import { WithMetadata, ErrorAction } from '../../actions/middleware/types';
import { ITelemetryEvent, TelemetryPropertyValue } from './types';
export class ActionErrorEvent implements ITelemetryEvent {
    readonly name: string = 'vsonline/action/failure';
    constructor(private readonly action: WithMetadata<ErrorAction>) {}
    getProperties(
        defaultProperties: Record<string, TelemetryPropertyValue>
    ): Record<string, TelemetryPropertyValue> {
        return {
            ...defaultProperties,
            ...this.action.metadata.telemetryProperties,
            action: this.action.type,
            correlationId: this.action.metadata.correlationId,
        };
    }
}

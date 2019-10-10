import { WithMetadata, BaseAction } from '../../actions/middleware/types';
import { ITelemetryEvent, TelemetryPropertyValue } from './types';
export class ActionSuccessEvent implements ITelemetryEvent {
    readonly name: string = 'vsonline/action/success';
    constructor(private readonly action: WithMetadata<BaseAction>) {}
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

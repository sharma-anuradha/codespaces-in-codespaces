import { WithMetadata, BaseAction } from '../../actions/middleware/types';
import { ITelemetryEvent } from './types';
export class ActionSuccessEvent implements ITelemetryEvent {
    readonly name: string = 'vsonline/action/success';
    constructor(private readonly action: WithMetadata<BaseAction>) {}
    get properties() {
        return {
            ...this.action.metadata.telemetryProperties,
            action: this.action.type,
            correlationId: this.action.metadata.correlationId,
            date: this.action.date,
        };
    }
}

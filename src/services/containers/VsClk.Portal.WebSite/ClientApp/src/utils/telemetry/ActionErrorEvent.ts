import { WithMetadata, ErrorAction } from '../../actions/middleware/types';
import { ITelemetryEvent } from './types';

export class ActionErrorEvent implements ITelemetryEvent {
    readonly name: string = 'vsonline/action/failure';
    constructor(private readonly action: WithMetadata<ErrorAction>) {}
    get properties() {
        return {
            ...this.action.metadata.telemetryProperties,
            action: this.action.type,
            correlationId: this.action.metadata.correlationId,
        };
    }
}

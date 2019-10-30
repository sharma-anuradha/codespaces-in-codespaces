import { MiddlewareAPI } from 'redux';
import { DispatchWithContext } from '../actions/middleware/types';
import { ApplicationState } from '../reducers/rootReducer';
import { TelemetryPropertyValue } from '../utils/telemetry/types';

export interface IActionContext {
    readonly makeRequest: typeof fetch;
    test_setMockRequestFactory: (obj: typeof fetch) => void;
    storeApi?: MiddlewareAPI<DispatchWithContext, ApplicationState>;
    readonly state: ApplicationState;
    test_setApplicationState: (state: Partial<ApplicationState>) => void;
    dispatch: (action: any) => any;
    readonly __id: string;
    readonly __instanceId: string;
    getTelemetryProperties: (obj: Record<string, TelemetryPropertyValue>) => any;
    clone(): IActionContext;
}

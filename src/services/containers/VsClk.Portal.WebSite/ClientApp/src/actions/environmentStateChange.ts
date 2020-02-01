import { action } from './middleware/useActionCreator';
import { StateInfo } from '../interfaces/cloudenvironment';
import { useActionContext } from './middleware/useActionContext';

export const stateChangeEnvironmentActionType = 'async.environments.stateChange';

// Basic actions dispatched for reducers
export const stateChangeEnvironmentAction = (
    id: string,
    environmentState: StateInfo,
    oldState?: StateInfo
) => {
    const context = useActionContext();
    context.setContextTelemetryProperty('environmentid', id);
    context.setContextTelemetryProperty('state', environmentState);
    context.setContextTelemetryProperty('oldState', oldState);
    return action(stateChangeEnvironmentActionType, { id, environmentState, oldState });
};

// Types to register with reducers
export type StateChangeEnvironmentAction = ReturnType<typeof stateChangeEnvironmentAction>;
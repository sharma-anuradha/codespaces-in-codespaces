import { action } from './middleware/useActionCreator';
import { StateInfo } from '../interfaces/cloudenvironment';

export const stateChangeEnvironmentActionType = 'async.environments.stateChange';

// Basic actions dispatched for reducers
export const stateChangeEnvironmentAction = (id: string, environmentState: StateInfo) =>
    action(stateChangeEnvironmentActionType, { id, environmentState });

// Types to register with reducers
export type StateChangeEnvironmentAction = ReturnType<typeof stateChangeEnvironmentAction>;

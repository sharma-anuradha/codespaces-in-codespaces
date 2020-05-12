import { action } from './middleware/useActionCreator';

import { IEnvironment } from 'vso-client-core';

export const environmentChangedActionType = 'async.environments.environmentChanged';

// Basic actions dispatched for reducers
export const environmentChangedAction = (environment: IEnvironment) =>
    action(environmentChangedActionType, { environment });

// Types to register with reducers
export type EnvironmentChangedAction = ReturnType<typeof environmentChangedAction>;

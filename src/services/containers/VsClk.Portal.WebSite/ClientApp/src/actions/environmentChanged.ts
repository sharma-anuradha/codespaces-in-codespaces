import { action } from './middleware/useActionCreator';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';

export const environmentChangedActionType = 'async.environments.environmentChanged';

// Basic actions dispatched for reducers
export const environmentChangedAction = (environment: ICloudEnvironment) =>
    action(environmentChangedActionType, { environment });

// Types to register with reducers
export type EnvironmentChangedAction = ReturnType<typeof environmentChangedAction>;

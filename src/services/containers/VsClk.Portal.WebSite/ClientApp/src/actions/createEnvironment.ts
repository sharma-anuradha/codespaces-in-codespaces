import envRegService, { CreateEnvironmentParameters } from '../services/envRegService';
import { action, Dispatch } from './actionUtils';
import { createUniqueId } from '../dependencies';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';
import { pollEnvironment } from './pollEnvironment';
import { ReduxAuthenticationProvider } from './reduxAuthenticationProvider';

export const createEnvironmentActionType = 'async.environments.create';
export const createEnvironmentSuccessActionType = 'async.environments.create.success';
export const createEnvironmentFailureActionType = 'async.environments.create.failure';

// Basic actions dispatched for reducers
const createEnvironmentAction = (lieId: string, environment: CreateEnvironmentParameters) =>
    action(createEnvironmentActionType, { lieId, environment });
const createEnvironmentSuccessAction = (lieId: string, environment: ICloudEnvironment) =>
    action(createEnvironmentSuccessActionType, { lieId, environment });
const createEnvironmentFailureAction = (lieId: string, error: Error) =>
    action(createEnvironmentFailureActionType, { lieId }, error);

// Types to register with reducers
export type CreateEnvironmentAction = ReturnType<typeof createEnvironmentAction>;
export type CreateEnvironmentSuccessAction = ReturnType<typeof createEnvironmentSuccessAction>;
export type CreateEnvironmentFailureAction = ReturnType<typeof createEnvironmentFailureAction>;

// Exposed - callable actions that have side-effects
export const createEnvironment = (parameters: CreateEnvironmentParameters) => async (
    dispatch: Dispatch
) => {
    // Have a lieId so we can identify the instance for optimistic UI updates.
    const lieId = createUniqueId();

    // 1. Try to create environment
    try {
        dispatch(createEnvironmentAction(lieId, parameters));
        const environment = await envRegService.createEnvironment(
            parameters,
            new ReduxAuthenticationProvider(dispatch)
        );
        dispatch(createEnvironmentSuccessAction(lieId, environment));
        try {
            dispatch(pollEnvironment(environment.id));
        } catch {
            // Noop
        }
    } catch (err) {
        dispatch(createEnvironmentFailureAction(lieId, err));
    }
};

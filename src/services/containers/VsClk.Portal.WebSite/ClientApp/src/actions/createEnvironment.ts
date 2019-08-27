import {
    createEnvironment as createCloudEnvironment,
    CreateEnvironmentParameters,
} from '../services/envRegService';
import { createUniqueId } from '../dependencies';
import { ICloudEnvironment } from '../interfaces/cloudenvironment';
import { pollEnvironment } from './pollEnvironment';
import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';

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
export async function createEnvironment(parameters: CreateEnvironmentParameters) {
    const dispatch = useDispatch();

    // Have a lieId so we can identify the instance for optimistic UI updates.
    const lieId = createUniqueId();

    // 1. Try to create environment
    try {
        dispatch(createEnvironmentAction(lieId, parameters));
        const environment = await createCloudEnvironment(parameters);
        dispatch(createEnvironmentSuccessAction(lieId, environment));
        try {
            dispatch(pollEnvironment(environment.id));
        } catch (err) {
            // Noop
        }
    } catch (err) {
        dispatch(createEnvironmentFailureAction(lieId, err));
    }
}

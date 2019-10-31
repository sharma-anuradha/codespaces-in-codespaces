import {
    createEnvironment as createCloudEnvironment,
    CreateEnvironmentParameters,
} from '../services/envRegService';
import { createUniqueId } from '../dependencies';
import { ICloudEnvironment, EnvironmentErrorCodes } from '../interfaces/cloudenvironment';
import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';
import { getUserInfo } from './getUserInfo';
import { ServiceResponseError } from './middleware/useWebClient';
import { environmentErrorCodeToString } from '../utils/environmentUtils';

type PartialEnvironmentInfo = Omit<CreateEnvironmentParameters, 'userEmail' | 'userName'>;

export const createEnvironmentActionType = 'async.environments.create';
export const createEnvironmentSuccessActionType = 'async.environments.create.success';
export const createEnvironmentFailureActionType = 'async.environments.create.failure';

// Basic actions dispatched for reducers
const createEnvironmentAction = (lieId: string, environment: PartialEnvironmentInfo) =>
    action(createEnvironmentActionType, { lieId, environment });
const createEnvironmentSuccessAction = (lieId: string, environment: ICloudEnvironment) =>
    action(createEnvironmentSuccessActionType, { lieId, environment });
const createEnvironmentFailureAction = (lieId: string, errorMessage: string, error: Error) =>
    action(createEnvironmentFailureActionType, { lieId, errorMessage }, error);

// Types to register with reducers
export type CreateEnvironmentAction = ReturnType<typeof createEnvironmentAction>;
export type CreateEnvironmentSuccessAction = ReturnType<typeof createEnvironmentSuccessAction>;
export type CreateEnvironmentFailureAction = ReturnType<typeof createEnvironmentFailureAction>;

// Exposed - callable actions that have side-effects
export async function createEnvironment(parameters: PartialEnvironmentInfo) {
    const dispatch = useDispatch();

    // Have a lieId so we can identify the instance for optimistic UI updates.
    const lieId = createUniqueId();

    try {
        // 1. We can start lying immediately.
        dispatch(createEnvironmentAction(lieId, parameters));

        // 2. Get details about the user
        const userInfo = await getUserInfo();
        if (!userInfo) {
            throw new Error('Failed to get current user info.');
        }

        const environmentParameters: CreateEnvironmentParameters = {
            ...parameters,
            userEmail: userInfo.mail,
            userName: userInfo.displayName,
        };

        // 3. Try to create the environment
        const environment = await createCloudEnvironment(environmentParameters);
        dispatch(createEnvironmentSuccessAction(lieId, environment));
    } catch (err) {
        if (err instanceof ServiceResponseError) {
            const code = (await err.response.json()) as EnvironmentErrorCodes;

            err.message = environmentErrorCodeToString(code);

            dispatch(
                createEnvironmentFailureAction(lieId, environmentErrorCodeToString(code), err)
            );
        }

        dispatch(createEnvironmentFailureAction(lieId, err.errorMessage, err));
    }
}

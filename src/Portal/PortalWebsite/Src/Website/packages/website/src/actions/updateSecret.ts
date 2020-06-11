import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';
import { useActionContext } from './middleware/useActionContext';
import { useWebClient, ServiceResponseError } from './middleware/useWebClient';
import { ISecret, IUpdateSecretRequest } from 'vso-client-core';

export const updateSecretActionType = 'async.secret.update';
export const updateSecretSuccessActionType = 'async.secret.update.success';
export const updateSecretFailureActionType = 'async.secret.update.failure';

// Basic actions dispatched for reducers
const updateSecretAction = () => action(updateSecretActionType);
const updateSecretSuccessAction = (secret: ISecret) =>
    action(updateSecretSuccessActionType, { secret });
const updateSecretFailureAction = (error: Error) => action(updateSecretFailureActionType, error);

// Types to register with reducers
export type UpdateSecretAction = ReturnType<typeof updateSecretAction>;
export type UpdateSecretSuccessAction = ReturnType<typeof updateSecretSuccessAction>;
export type UpdateSecretFailureAction = ReturnType<typeof updateSecretFailureAction>;

// Exposed - callable actions that have side-effects
export async function updateSecret(
    planId: string,
    updateSecretRequest: IUpdateSecretRequest,
    secretId: string
) {
    const dispatch = useDispatch();

    const { state } = useActionContext();
    const { configuration } = state;

    if (!configuration) {
        throw new Error('Fetch configuration first.');
    }

    const { apiEndpoint } = configuration;
    try {
        dispatch(updateSecretAction());

        const webClient = useWebClient();

        const secret = (await webClient.put<ISecret>(
            `${apiEndpoint}/secrets/${secretId}?planId=${encodeURIComponent(planId)}`,
            updateSecretRequest
        )) as ISecret;

        return dispatch(updateSecretSuccessAction(secret));
    } catch (err) {
        if (err instanceof ServiceResponseError) {
            const errorCode = await err.response.json();
            if (errorCode) {
                err.message = errorCode;
            }
        }
        return dispatch(updateSecretFailureAction(err));
    }
}

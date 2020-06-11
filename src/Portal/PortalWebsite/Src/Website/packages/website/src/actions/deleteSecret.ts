import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';
import { useActionContext } from './middleware/useActionContext';
import { useWebClient, ServiceResponseError } from './middleware/useWebClient';
import { SecretScope } from 'vso-client-core';

export const deleteSecretActionType = 'async.secret.delete';
export const deleteSecretSuccessActionType = 'async.secret.delete.success';
export const deleteSecretFailureActionType = 'async.secret.delete.failure';

// Basic actions dispatched for reducers
const deleteSecretAction = () => action(deleteSecretActionType);
const deleteSecretSuccessAction = (secretId: string) =>
    action(deleteSecretSuccessActionType, { secretId });
const deleteSecretFailureAction = (error: Error) => action(deleteSecretFailureActionType, error);

// Types to register with reducers
export type DeleteSecretAction = ReturnType<typeof deleteSecretAction>;
export type DeleteSecretSuccessAction = ReturnType<typeof deleteSecretSuccessAction>;
export type DeleteSecretFailureAction = ReturnType<typeof deleteSecretFailureAction>;

// Exposed - callable actions that have side-effects
export async function deleteSecret(planId: string, secretId: string, secretScope: SecretScope) {
    const dispatch = useDispatch();

    const { state } = useActionContext();
    const { configuration } = state;

    if (!configuration) {
        throw new Error('Fetch configuration first.');
    }

    const { apiEndpoint } = configuration;
    try {
        dispatch(deleteSecretAction());

        const webClient = useWebClient();

        await webClient.delete(
            `${apiEndpoint}/secrets/${secretId}?planId=${encodeURIComponent(
                planId
            )}&scope=${encodeURIComponent(secretScope)}`
        );

        dispatch(deleteSecretSuccessAction(secretId));
    } catch (err) {
        if (err instanceof ServiceResponseError) {
            const errorCode = await err.response.json();
            if (errorCode) {
                err.message = errorCode;
            }
        }
        return dispatch(deleteSecretFailureAction(err));
    }
}
